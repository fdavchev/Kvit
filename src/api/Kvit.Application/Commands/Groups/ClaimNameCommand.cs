namespace Kvit.Application.Commands.Groups
{
    public sealed record ClaimNameCommand(Guid GroupId, Guid MemberId);
}
