using Kvit.Domain.Settlements;

namespace Kvit.Application.Commands.Settlements
{
    public sealed record RecordSettlementCommand(Guid GroupId, Guid ClientRequestId, SettlementInput Input);
}
