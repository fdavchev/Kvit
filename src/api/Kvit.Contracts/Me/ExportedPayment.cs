using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Me
{
    public sealed record ExportedPayment(
        Guid Id,
        Guid FromMemberId,
        Guid ToMemberId,
        long AmountMinor,
        Currency Currency,
        SettlementStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ResolvedAt);
}
