namespace Kvit.Application.Commands.Groups
{
    public sealed record RemoveMemberCommand(Guid GroupId, Guid MemberId);
}
