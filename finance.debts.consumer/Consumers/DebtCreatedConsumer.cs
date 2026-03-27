using finance.debts.consumer.Exceptions;
using finance.debts.consumer.Infrastructure.Repositories.External;
using finance.debts.Contracts;
using MassTransit;
using System.Diagnostics;

namespace finance.debts.consumer.Consumers
{
    public class DebtCreatedConsumer : IConsumer<IDebtCreated>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DebtCreatedConsumer> _logger;

        public DebtCreatedConsumer(IServiceProvider serviceProvider, ILogger<DebtCreatedConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IDebtCreated> context) 
        {
            var message = context.Message; 
            var correlationId = context.CorrelationId?.ToString() ?? Guid.NewGuid().ToString();

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("📥 Mensagem recebida | CorrelationId: {CorrelationId}", correlationId);

                if (message == null)
                    throw new BusinessException("Mensagem inválida");

                var debtId = message.DebtId;

                if (debtId <= 0)
                    throw new BusinessException("DebtId inválido");

                _logger.LogInformation(
                    "🔎 Processando DebtId: {DebtId} | CorrelationId: {CorrelationId}",
                    debtId,
                    correlationId
                );

                using var scope = _serviceProvider.CreateScope();
                var api = scope.ServiceProvider.GetRequiredService<DebtApi>();

                var result = await api.ProcessDebtAsync(debtId, correlationId);

                if (result.Contains("already processed"))
                {
                    _logger.LogWarning("🔁 Já processado | DebtId: {DebtId}", debtId);
                }
                else
                {
                    _logger.LogInformation("✅ Processado com sucesso | DebtId: {DebtId}", debtId);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "⏱ Tempo de processamento: {Elapsed}ms | DebtId: {DebtId}",
                    stopwatch.ElapsedMilliseconds,
                    debtId
                );
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex,
                    "⚠️ Regra de negócio | CorrelationId: {CorrelationId}",
                    correlationId);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "💥 Erro inesperado | CorrelationId: {CorrelationId}",
                    correlationId);

                throw;
            }
        }
    }
}