namespace Kvit.Application.Commands.Groups
{
    public sealed record AddMemberCommand(Guid GroupId, string Name);
}
