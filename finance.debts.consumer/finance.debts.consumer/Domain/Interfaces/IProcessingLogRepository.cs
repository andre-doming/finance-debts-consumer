using finance.debts.domain.Entities;

namespace finance.debts.consumer.Domain.Interfaces
{
    public interface IProcessingLogRepositoryxx
    {
        Task AddAsync(ProcessingLog log);
    }
}
