namespace Kvit.Contracts.Invites
{
    public sealed record JoinGroupRequest(string Token, Guid? ClaimMemberId);
}
