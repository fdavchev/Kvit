using Kvit.Domain.MoneyRules;

namespace Kvit.Application.Queries.GroupSpending
{
    public sealed record StoredPlanDto(long? AmountMinor, Currency? Currency);
}
