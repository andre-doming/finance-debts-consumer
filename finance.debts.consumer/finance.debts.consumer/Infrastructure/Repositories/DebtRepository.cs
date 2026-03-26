using Dapper;
using Microsoft.Data.SqlClient;
using finance.debts.consumer.Domain.Entities;
using finance.debts.consumer.Domain.Interfaces;

namespace finance.debts.consumer.Infrastructure.Repositories
{
    public class DebtRepository : IDebtRepository
    {
        private readonly string _connectionString;

        public DebtRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task<Debt?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
            SELECT
                debt_id AS DebtId,
                client_id AS ClientId,
                amount_due AS AmountDue,
                status_id AS StatusId,
                created_at AS CreatedAt
            FROM Debts
            WHERE debt_id = @Id";

            return await connection.QueryFirstOrDefaultAsync<Debt>(sql, new { Id = id });
        }
        public async Task UpdateAsync(Debt debt)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
            UPDATE Debts
            SET status_id = @StatusId,
            amount_paid = @AmountPaid,
            payment_date = @PaymentDate
            WHERE debt_id = @DebtId";

            var rows = await connection.ExecuteAsync(sql, new
            {
                debt.StatusId,
                debt.AmountPaid,
                debt.PaymentDate,
                debt.DebtId
            });

            if (rows == 0)
                throw new Exception("Falha ao atualizar dívida");
        }
    }
}