namespace Kvit.Application.Commands.Groups
{
    public sealed record LetGuestInAgainCommand(Guid GroupId, Guid MemberId);
}
