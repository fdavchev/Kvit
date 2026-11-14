namespace Kvit.Application.Commands.OwnerTools
{
    public sealed record CloseGroupAsAdminCommand(Guid GroupId, string? Reason);
}
