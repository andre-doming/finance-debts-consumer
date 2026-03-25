using finance.debts.consumer.Domain.Events;
using finance.debts.consumer.Domain.Exceptions;
using finance.debts.consumer.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace finance.debts.consumer
{
    public class Worker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Worker> _logger;

        public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 WORKER STARTOU");

            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                VirtualHost = "finance",
                UserName = "svc-debts",
                Password = "SvcDebts!7pQ3a"
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            // 🔥 GARANTE QUE O EXCHANGE EXISTE
            channel.ExchangeDeclare(
                exchange: "finance.debts.created",
                type: ExchangeType.Fanout,
                durable: true
            );

            // 🔥 FILA PRINCIPAL (com DLQ)
            var mainArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "" },
                { "x-dead-letter-routing-key", "finance.debts-dlq.queue" }
            };

            channel.QueueDeclare(
                queue: "finance.debts.queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: mainArgs
            );

            _logger.LogInformation("🔗 Binding queue...");
            channel.QueueBind(
                queue: "finance.debts.queue",
                exchange: "finance.debts.created",
                routingKey: ""
            );
            _logger.LogInformation("✅ Binding realizado");

            // 🔥 DLQ
            channel.QueueDeclare(
                queue: "finance.debts-dlq.queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            // 🔥 RETRY QUEUE (com TTL)
            var retryArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "" },
                { "x-dead-letter-routing-key", "finance.debts.queue" },
                { "x-message-ttl", 5000 }
            };

            channel.QueueDeclare(
                queue: "finance.debts-retry.queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: retryArgs
            );

            // 🔥 MANUAL
            channel.QueueDeclare(
                queue: "finance.debts-manual.queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (model, ea) =>
            {
                var json = string.Empty;

                try
                {
                    var body = ea.Body.ToArray();
                    json = Encoding.UTF8.GetString(body);

                    _logger.LogInformation($"📩 Mensagem recebida: {json}");

                    var wrapper = JsonSerializer.Deserialize<DebtCreatedEventWrapper>(json);

                    if (wrapper?.message == null)
                        throw new BusinessException("Mensagem inválida");

                    var debtId = wrapper.message.debtId;

                    if (debtId <= 0)
                        throw new BusinessException("DebtId inválido");

                    // conversão segura
                    var amount = decimal.Parse(
                        wrapper.message.AmountDue,
                        CultureInfo.InvariantCulture
                    );

                    using var scope = _serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<DebtService>();

                    await service.ProcessDebtAsync(debtId);

                    // ✅ SUCESSO → ACK
                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (BusinessException ex)
                {
                    _logger.LogWarning(ex, "⚠️ Regra de negócio");

                    PublishToManualQueue(channel, json, ex);

                    // remove da fila principal
                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Erro técnico");

                    HandleRetry(channel, ea, json);
                }
            };

            channel.BasicConsume(
                queue: "finance.debts.queue",
                autoAck: false,
                consumer: consumer);

            // 🔥 MANTÉM O WORKER VIVO (ESSENCIAL)
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        // 🔁 RETRY COM CONTADOR
        private void HandleRetry(IModel channel, BasicDeliverEventArgs ea, string json)
        {
            var props = channel.CreateBasicProperties();
            props.Headers = new Dictionary<string, object>();

            int retryCount = 0;

            if (ea.BasicProperties.Headers != null &&
                ea.BasicProperties.Headers.ContainsKey("x-retry-count"))
            {
                retryCount = Convert.ToInt32(ea.BasicProperties.Headers["x-retry-count"]);
            }

            retryCount++;

            if (retryCount >= 3)
            {
                _logger.LogWarning("❌ Excedeu tentativas → DLQ");

                channel.BasicNack(ea.DeliveryTag, false, false);
                return;
            }

            props.Headers["x-retry-count"] = retryCount;

            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(
                exchange: "",
                routingKey: "finance.debts-retry.queue",
                basicProperties: props,
                body: body
            );

            _logger.LogWarning($"🔁 Retry {retryCount}/3");

            channel.BasicAck(ea.DeliveryTag, false);
        }

        private void PublishToManualQueue(IModel channel, string originalJson, Exception ex)
        {
            var payload = new
            {
                originalMessage = originalJson,
                error = ex.Message,
                createdAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(payload);
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(
                exchange: "",
                routingKey: "finance.debts-manual.queue",
                basicProperties: null,
                body: body
            );
        }
    }
}