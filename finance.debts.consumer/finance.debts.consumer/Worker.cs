using finance.debts.consumer.Domain.Events;
using finance.debts.consumer.Domain.Exceptions;
using finance.debts.consumer.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Globalization;
using System.Text;
using System.Text.Json;
using finance.debts.consumer.Services.External;

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

            // 🔥 NOVO: CONTROLE DE CONCORRÊNCIA
            channel.BasicQos(0, 1, false);

            channel.ExchangeDeclare("finance.debts.created", ExchangeType.Fanout, true);

            var mainArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "" },
                { "x-dead-letter-routing-key", "finance.debts-dlq.queue" }
            };

            channel.QueueDeclare("finance.debts.queue", true, false, false, mainArgs);

            channel.QueueBind("finance.debts.queue", "finance.debts.created", "");

            channel.QueueDeclare("finance.debts-dlq.queue", true, false, false);

            var retryArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "" },
                { "x-dead-letter-routing-key", "finance.debts.queue" }
            };

            channel.QueueDeclare("finance.debts-retry.queue", true, false, false, retryArgs);

            channel.QueueDeclare("finance.debts-manual.queue", true, false, false);

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (model, ea) =>
            {
                var json = string.Empty;

                try
                {
                    var body = ea.Body.ToArray();
                    json = Encoding.UTF8.GetString(body);

                    var wrapper = JsonSerializer.Deserialize<DebtCreatedEventWrapper>(json);

                    // 🔥 CORRETO (único bloco de correlationId)
                    var correlationId = wrapper?.correlationId?.ToString();

                    if (string.IsNullOrEmpty(correlationId))
                    {
                        correlationId = ea.BasicProperties?.CorrelationId;
                    }

                    if (string.IsNullOrEmpty(correlationId))
                    {
                        correlationId = Guid.NewGuid().ToString();
                    }

                    if (wrapper?.message == null)
                        throw new BusinessException("Mensagem inválida");

                    var debtId = wrapper.message.debtId;

                    //_logger.LogInformation("📩 DebtId recebido: {DebtId}", debtId);
                    _logger.LogInformation(
                    "📩 DebtId: {DebtId} | CorrelationId: {CorrelationId}",
                    debtId,
                    correlationId
                    );

                    if (debtId <= 0)
                        throw new BusinessException("DebtId inválido");

                    using var scope = _serviceProvider.CreateScope();
                    var api = scope.ServiceProvider.GetRequiredService<DebtApi>();

                    var result = await api.ProcessDebtAsync(debtId, correlationId);

                    if (result.Contains("already processed"))
                    {
                        _logger.LogInformation("🔁 Já processado: {DebtId}", debtId);
                    }

                    // ✅ SUCESSO
                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning(ex, "⏱ Timeout na API - retry");

                    HandleRetry(channel, ea, json);
                }
                catch (BusinessException ex)
                {
                    // ⚠️ erro de negócio
                    _logger.LogWarning(ex, "⚠️ Regra de negócio");

                    PublishToManualQueue(channel, json, ex);

                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    // 💥 fallback (tratado como erro de negócio)
                    _logger.LogWarning(ex, "⚠️ Erro não mapeado");

                    PublishToManualQueue(channel, json, ex);

                    channel.BasicAck(ea.DeliveryTag, false);
                }

            };

            channel.BasicConsume("finance.debts.queue", false, consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }
        }

        private void HandleRetry(IModel channel, BasicDeliverEventArgs ea, string json)
        {
            var props = channel.CreateBasicProperties();
            props.Headers = ea.BasicProperties.Headers ?? new Dictionary<string, object>();

            int retryCount = 0;

            if (props.Headers.ContainsKey("x-retry-count"))
            {
                retryCount = Convert.ToInt32(props.Headers["x-retry-count"]);
            }

            retryCount++;

            if (retryCount >= 3)
            {
                _logger.LogWarning("❌ Excedeu tentativas → DLQ");

                channel.BasicNack(ea.DeliveryTag, false, false);
                return;
            }

            props.Headers["x-retry-count"] = retryCount;

            // 🔥 BACKOFF PROGRESSIVO
            int delay = retryCount switch
            {
                1 => 5000,   // 5s
                2 => 15000,  // 15s
                3 => 30000,  // 30s
                _ => 5000
            };

            props.Expiration = delay.ToString(); // TTL por mensagem

            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(
                exchange: "",
                routingKey: "finance.debts-retry.queue",
                basicProperties: props,
                body: body
            );

            _logger.LogWarning("🔁 Retry {Retry} em {Delay}ms", retryCount, delay);

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

            channel.BasicPublish("", "finance.debts-manual.queue", null, body);
        }
    }
}