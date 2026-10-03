namespace Kvit.Api.Tests.Groups
{
    public sealed record GroupSetup(Guid GroupId, string InviteToken, SignedInUser Owner, Guid OwnerRowId, SignedInUser Member, Guid MemberRowId);
}
