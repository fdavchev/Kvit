using Kvit.Domain.MoneyRules;

namespace Kvit.Application.Queries.GroupSpending
{
    public sealed record CategorySpendingDto(Currency Currency, Guid? CategoryId, int? CategorySortOrder, long TotalMinor);
}
