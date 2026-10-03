namespace Kvit.Application.Commands.Invites
{
    public sealed record JoinGroupCommand(string Token, Guid? ClaimMemberId);
}
