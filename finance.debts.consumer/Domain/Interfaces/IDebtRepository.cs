using finance.debts.domain.Entities;

namespace finance.debts.consumer.Domain.Interfaces
{
    public interface IDebtRepositoryxx
    {
        Task<Debt?> GetByIdAsync(int id);
        Task UpdateAsync(Debt debt);
    }
}