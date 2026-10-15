using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Settlements;

namespace Kvit.Api.Tests.Guards
{
    public delegate Task<SendWrite> PrepareWrite(ExpensesApp app, SettlementGroup group);
}
