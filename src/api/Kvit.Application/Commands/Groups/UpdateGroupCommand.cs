namespace Kvit.Application.Commands.Groups
{
    public sealed record UpdateGroupCommand(Guid GroupId, string Name, string Emoji, string Currency);
}
