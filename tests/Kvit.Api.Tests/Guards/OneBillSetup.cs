using Kvit.Api.Tests.Groups;

namespace Kvit.Api.Tests.Guards
{
    public sealed record OneBillSetup(Guid GroupId, SignedInUser Owner, Guid OwnerRowId, Guid MarkoRowId, Guid ExpenseId);
}
