namespace Kvit.Application.Commands.Settlements
{
    public sealed record DeleteSettlementCommand(Guid GroupId, Guid SettlementId);
}
