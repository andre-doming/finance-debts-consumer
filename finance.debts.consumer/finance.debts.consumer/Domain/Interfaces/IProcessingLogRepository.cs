using finance.debts.consumer.Domain.Entities;

namespace finance.debts.consumer.Domain.Interfaces
{
    public interface IProcessingLogRepository
    {
        Task AddAsync(ProcessingLog log);
    }
}
