using Kvit.Api.Tests.Groups;

namespace Kvit.Api.Tests.Closing
{
    public sealed record LoneOwnerGroup(Guid GroupId, SignedInUser Owner, Guid OwnerRowId);
}
