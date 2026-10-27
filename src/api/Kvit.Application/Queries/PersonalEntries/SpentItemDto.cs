using Kvit.Domain.MoneyRules;

namespace Kvit.Application.Queries.PersonalEntries
{
    public sealed record SpentItemDto(Guid? CategoryId, long AmountMinor, Currency Currency, decimal MkdPerEur);
}
