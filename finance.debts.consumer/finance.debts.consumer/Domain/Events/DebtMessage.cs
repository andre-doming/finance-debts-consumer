using System.Text.Json.Serialization;

namespace finance.debts.consumer.Domain.Events
{
    public class DebtMessage
    {
        public int debtId { get; set; }
        public int clientId { get; set; }
        [JsonPropertyName("amountDue")]
        public string AmountDue { get; set; } = string.Empty;
        public int statusId { get; set; }
        public DateTime createdAt { get; set; }
    }
}