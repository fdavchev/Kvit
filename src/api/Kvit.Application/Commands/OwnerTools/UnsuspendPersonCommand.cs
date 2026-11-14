namespace Kvit.Application.Commands.OwnerTools
{
    public sealed record UnsuspendPersonCommand(Guid PersonId, string? Reason);
}
