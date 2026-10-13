using Kvit.Contracts.Validation;

namespace Kvit.Contracts.Settlements
{
    public sealed record RecordSettlementRequest(
        [NotEmptyGuid] Guid ClientRequestId,
        Guid FromMemberId,
        Guid ToMemberId,
        long AmountMinor,
        string Currency);
}
