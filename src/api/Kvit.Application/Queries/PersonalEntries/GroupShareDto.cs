using Kvit.Contracts.PersonalEntries;

namespace Kvit.Application.Queries.PersonalEntries
{
    public sealed record GroupShareDto(GroupShareRow Row, decimal MkdPerEur, long ExpenseAmountMinor, bool IsPaidByCaller);
}
