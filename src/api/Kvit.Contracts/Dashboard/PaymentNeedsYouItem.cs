using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Dashboard
{
    public sealed record PaymentNeedsYouItem(Guid GroupId, string GroupName, Guid SettlementId, string FromName, long AmountMinor, Currency Currency)
        : NeedsYouItem(GroupId, GroupName);
}
