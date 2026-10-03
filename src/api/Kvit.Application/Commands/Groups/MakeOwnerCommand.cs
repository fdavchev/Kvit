namespace Kvit.Application.Commands.Groups
{
    public sealed record MakeOwnerCommand(Guid GroupId, Guid MemberId);
}
