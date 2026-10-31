using Kvit.Domain.MoneyRules;

namespace Kvit.Domain.Entities
{
    public sealed record DuplicateMatchFields(long AmountMinor, Currency Currency, Guid? CategoryId, DateOnly EntryDate);
}
