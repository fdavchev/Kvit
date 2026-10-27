using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.PersonalEntries
{
    public sealed record PossibleDuplicateRow(
        Guid EntryId,
        string? EntryNote,
        long EntryAmountMinor,
        DateOnly EntryDate,
        Guid EntryCategoryId,
        Guid ExpenseId,
        Guid GroupId,
        string GroupName,
        string? ExpenseTitle,
        DateOnly ExpenseDate,
        Currency Currency,
        long ShareMinor,
        long ExpenseAmountMinor,
        PossibleDuplicateMatch MatchedOn);
}
