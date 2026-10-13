using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Settlements
{
    public sealed record SettlementResponse(
        Guid Id,
        Guid FromMemberId,
        string FromName,
        Guid ToMemberId,
        string ToName,
        long AmountMinor,
        Currency Currency,
        SettlementStatus Status,
        Guid RecordedByUserId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ResolvedAt);
}
