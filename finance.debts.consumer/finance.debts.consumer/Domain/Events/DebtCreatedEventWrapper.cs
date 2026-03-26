using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace finance.debts.consumer.Domain.Events
{
    public class DebtCreatedEventWrapper
    {
        public Guid? correlationId { get; set; }
        public DebtMessage message { get; set; }
    }
}
