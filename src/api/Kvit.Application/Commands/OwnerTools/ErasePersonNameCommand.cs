namespace Kvit.Application.Commands.OwnerTools
{
    public sealed record ErasePersonNameCommand(Guid PersonId, string? Reason);
}
