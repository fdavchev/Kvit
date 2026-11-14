using Kvit.Api.Tests.Expenses;

namespace Kvit.Api.Tests.Cleanup
{
    public sealed record FullGroup(
        ExpenseGroup Group,
        Guid OwnerExpenseId,
        Guid MemberExpenseId,
        Guid SettlementId,
        Guid MemberEntryId)
    {
        public static readonly GroupRowCounts ExpectedCounts = new(
            Groups: 1,
            Members: 3,
            Expenses: 2,
            Shares: 4,
            Settlements: 1,
            ClosingResponses: 1,
            ActivityEvents: 4);
    }
}
