using Dapper;
using finance.debts.consumer.Domain.Entities;
using finance.debts.consumer.Domain.Interfaces;
using Microsoft.Data.SqlClient;

namespace finance.debts.consumer.Infrastructure.Repositories
{
    public class ProcessingLogRepository : IProcessingLogRepository
    {
        private readonly string _connectionString;

        public ProcessingLogRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task AddAsync(ProcessingLog log)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
        INSERT INTO processing_logs (debt_id, status_id, message, created_at)
        VALUES (@DebtId, @StatusId, @Message, @CreatedAt)";

            await connection.ExecuteAsync(sql, log);
        }
    }
}
