namespace Kvit.Application.Commands.Settlements
{
    public sealed record RejectSettlementCommand(Guid GroupId, Guid SettlementId);
}
