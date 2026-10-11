namespace Kvit.Application.Commands.Settlements
{
    public sealed record RestoreSettlementCommand(Guid GroupId, Guid SettlementId);
}
