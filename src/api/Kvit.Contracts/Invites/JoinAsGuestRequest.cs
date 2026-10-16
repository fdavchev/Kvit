namespace Kvit.Contracts.Invites
{
    public sealed record JoinAsGuestRequest(string Token, Guid? ClaimMemberId, string? Name, string Language, string TimeZone);
}
