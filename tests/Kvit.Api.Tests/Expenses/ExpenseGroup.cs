using Kvit.Api.Tests.Groups;

namespace Kvit.Api.Tests.Expenses
{
    public sealed record ExpenseGroup(
        Guid GroupId,
        SignedInUser Owner,
        Guid OwnerRowId,
        SignedInUser Member,
        Guid MemberRowId,
        IReadOnlyList<Guid> PlainRowIds)
    {
        public IReadOnlyList<Guid> RowIdsInJoiningOrder => [OwnerRowId, MemberRowId, .. PlainRowIds];
    }
}
