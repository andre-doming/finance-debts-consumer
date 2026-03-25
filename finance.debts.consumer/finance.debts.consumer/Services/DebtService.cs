using finance.debts.consumer.Domain.Entities;
using finance.debts.consumer.Domain.Enums;
using finance.debts.consumer.Domain.Interfaces;

namespace finance.debts.consumer.Services
{
    public class DebtService
    {
        private readonly IDebtRepository _repository;

        public DebtService(IDebtRepository repository)
        {
            _repository = repository;
        }

        private readonly IProcessingLogRepository _logRepository;
        public DebtService(IDebtRepository repository, IProcessingLogRepository logRepository)
        {
            _repository = repository;
            _logRepository = logRepository;
        }

        public async Task<string> ProcessDebtAsync(int id)
        {
            try
            {
                if (id <= 0)
                    throw new ArgumentException("DebtId inválido");

                var debt = await _repository.GetByIdAsync(id);

                if (debt is null)
                    throw new KeyNotFoundException("Dívida não encontrada");

                // regra de negócio
                if (debt.StatusId == ProcessingStatus.Processed)
                    throw new InvalidOperationException("Dívida já processada");

                // atualiza status
                debt.StatusId = ProcessingStatus.Processed;
                debt.AmountPaid= debt.AmountDue;
                debt.PaymentDate = DateTime.UtcNow;

                // 💾 salva no banco
                await _repository.UpdateAsync(debt);

                // 🟡 👉 AQUI ENTRA O LOG
                await _logRepository.AddAsync(new ProcessingLog
                {
                    DebtId = debt.DebtId,
                    StatusId = (int)debt.StatusId,
                    Message = "Dívida processada com sucesso",
                    CreatedAt = DateTime.UtcNow
                });

                return $"Debt {id} processed successfully";
            }
            catch (Exception ex)
            {
                await _logRepository.AddAsync(new ProcessingLog
                {
                    DebtId = id,
                    StatusId = -1,
                    Message = ex.Message,
                    CreatedAt = DateTime.UtcNow
                });

                throw;
            }
        }

        //Service fazia tudo sozinho
        //Service → chama Repository → usa Domain
    }
}