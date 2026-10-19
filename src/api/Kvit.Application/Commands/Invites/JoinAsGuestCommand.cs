namespace Kvit.Application.Commands.Invites
{
    public sealed record JoinAsGuestCommand(string Token, Guid? ClaimMemberId, string? Name, string Language, string TimeZone);
}
