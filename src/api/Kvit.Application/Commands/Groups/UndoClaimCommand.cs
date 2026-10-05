namespace Kvit.Application.Commands.Groups
{
    public sealed record UndoClaimCommand(Guid GroupId, Guid MemberId);
}
