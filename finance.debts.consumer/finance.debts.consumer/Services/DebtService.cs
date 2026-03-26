using finance.debts.consumer.Domain.Entities;
using finance.debts.consumer.Domain.Enums;
using finance.debts.consumer.Domain.Exceptions;
using finance.debts.consumer.Domain.Interfaces;

namespace finance.debts.consumer.Services
{
    public class DebtService
    {
        public async Task ProcessDebtAsync(int id)
        {
            if (id <= 0)
                throw new BusinessException("DebtId inválido");

            // aqui no futuro vai chamar API
            await Task.CompletedTask;
        }
    }
}