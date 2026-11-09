namespace Kvit.Application.Commands.OwnerTools
{
    public sealed record ReopenGroupAsAdminCommand(Guid GroupId, string? Reason);
}
