using finance.debts.consumer.Domain.Entities;

namespace finance.debts.consumer.Domain.Interfaces
{
    public interface IDebtRepository
    {
        Task<Debt?> GetByIdAsync(int id);
        Task UpdateAsync(Debt debt);
    }
}