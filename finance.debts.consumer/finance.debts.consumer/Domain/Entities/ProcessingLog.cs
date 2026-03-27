namespace finance.debts.consumer.Domain.Entities
{
    public class ProcessingLogxx
    {
        public int LogId { get; set; }
        public int DebtId { get; set; }
        public int StatusId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
