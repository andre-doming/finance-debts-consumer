using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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