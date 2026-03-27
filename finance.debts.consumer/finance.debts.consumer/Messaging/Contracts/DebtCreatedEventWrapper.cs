namespace finance.debts.consumer.Messaging.Contracts
{
    public class DebtCreatedEventWrapper
    {
        public Guid? correlationId { get; set; }
        public DebtMessage message { get; set; }
    }
}
