namespace Kvit.Application.Commands.OwnerTools
{
    public sealed record SuspendPersonCommand(Guid PersonId, string? Reason);
}
