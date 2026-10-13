namespace Kvit.Application.Commands.Settlements
{
    public sealed record ConfirmSettlementCommand(Guid GroupId, Guid SettlementId);
}
