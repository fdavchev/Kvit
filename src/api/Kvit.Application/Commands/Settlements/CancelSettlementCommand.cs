namespace Kvit.Application.Commands.Settlements
{
    public sealed record CancelSettlementCommand(Guid GroupId, Guid SettlementId);
}
