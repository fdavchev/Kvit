using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;

namespace Kvit.Api.Tests.AccountDeletion
{
    public sealed record DeletedPersonScenario(ExpenseGroup Group, SignedInUser Leaver, Guid ExpenseId, Guid SettlementId, string OldName);
}
