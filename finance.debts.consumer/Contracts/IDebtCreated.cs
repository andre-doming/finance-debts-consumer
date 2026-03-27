using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace finance.debts.Contracts
{
    public interface IDebtCreated
    {
        int DebtId { get; }
        decimal Amount { get; }
        string Document { get; }
        DateTime CreatedAt { get; }
    }
}
