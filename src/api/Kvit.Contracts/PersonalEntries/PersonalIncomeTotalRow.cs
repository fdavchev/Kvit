using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.PersonalEntries
{
    public sealed record PersonalIncomeTotalRow(Currency Currency, long TotalMinor, int Count);
}
