using finance.debts.consumer.Domain.Exceptions;

namespace finance.debts.consumer.Services
{
    public class DebtService
    {
        public async Task ProcessDebtAsync(int id)
        {
            if (id <= 0)
                throw new BusinessException("DebtId inválido");

            await Task.CompletedTask;
        }
    }
}