namespace finance.debts.consumer.Domain.Events
{
    public class DebtCreatedEventWrapper
    {
        public Guid? correlationId { get; set; }
        public DebtMessage message { get; set; }
    }
}
