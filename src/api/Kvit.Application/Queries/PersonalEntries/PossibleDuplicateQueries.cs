using Kvit.Contracts.PersonalEntries;
using Kvit.Domain.Entities;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.PersonalEntries
{
    public static class PossibleDuplicateQueries
    {
        private const int MaxDaysApart = 3;
        private const int AmountTolerancePercent = 10;
        private const int WholePercent = 100;

        public static async Task<List<PossibleDuplicateRow>> PossibleDuplicatesOfAsync(
            this AppDbContext context,
            Guid userId,
            DateOnly firstDay,
            DateOnly firstDayAfter,
            IReadOnlyList<PersonalEntryDto> entries,
            CancellationToken cancellationToken)
        {
            List<PersonalEntryDto> spending = [.. BudgetItems.CountedSpendingOf(entries)];
            if (spending.Count == 0)
            {
                return [];
            }

            List<GroupShareDto> candidateShares = await context
                .GroupSharesOf(userId, firstDay.AddDays(-MaxDaysApart), firstDayAfter.AddDays(MaxDaysApart))
                .ToListAsync(cancellationToken);
            if (candidateShares.Count == 0)
            {
                return [];
            }

            List<Guid> candidateExpenseIds = [.. candidateShares.Select(share => share.Row.ExpenseId)];
            List<Guid> mergedExpenseIds = await context.MergesOf(userId)
                .Where(merge => candidateExpenseIds.Contains(merge.ExpenseId))
                .Select(merge => merge.ExpenseId)
                .ToListAsync(cancellationToken);
            List<GroupShareDto> shares = [.. candidateShares.Where(share => !mergedExpenseIds.Contains(share.Row.ExpenseId))];
            if (shares.Count == 0)
            {
                return [];
            }

            List<Guid> entryIds = [.. spending.Select(entry => entry.Id)];
            List<DuplicateAnswer> answers = await context.DuplicateAnswers
                .AsNoTracking()
                .Where(answer => answer.UserId == userId && entryIds.Contains(answer.PersonalEntryId))
                .ToListAsync(cancellationToken);
            HashSet<(Guid EntryId, Guid ExpenseId)> answeredPairs = [.. answers.Select(answer => (answer.PersonalEntryId, answer.ExpenseId))];

            return [.. spending
                .SelectMany(entry => shares
                    .Where(share => !answeredPairs.Contains((entry.Id, share.Row.ExpenseId)))
                    .Select(share => PairOf(entry, share)))
                .OfType<PossibleDuplicateRow>()
                .OrderBy(pair => pair.EntryDate)
                .ThenBy(pair => pair.EntryId)
                .ThenBy(pair => pair.ExpenseDate)
                .ThenBy(pair => pair.ExpenseId)];
        }

        private static PossibleDuplicateRow? PairOf(PersonalEntryDto entry, GroupShareDto share)
        {
            GroupShareRow row = share.Row;
            if (row.CategoryId is not Guid categoryId
                || entry.CategoryId != categoryId
                || entry.Currency != row.Currency
                || Math.Abs(entry.EntryDate.DayNumber - row.EntryDate.DayNumber) > MaxDaysApart)
            {
                return null;
            }

            PossibleDuplicateMatch? match = MatchOf(entry.AmountMinor, share);
            if (match is not PossibleDuplicateMatch matchedOn)
            {
                return null;
            }

            return new PossibleDuplicateRow(
                entry.Id,
                entry.Note,
                entry.AmountMinor,
                entry.EntryDate,
                categoryId,
                row.ExpenseId,
                row.GroupId,
                row.GroupName,
                row.Title,
                row.EntryDate,
                row.Currency,
                row.ShareMinor,
                share.ExpenseAmountMinor,
                matchedOn);
        }

        private static PossibleDuplicateMatch? MatchOf(long entryAmountMinor, GroupShareDto share)
        {
            if (IsWithinTolerance(entryAmountMinor, share.Row.ShareMinor))
            {
                return PossibleDuplicateMatch.Share;
            }

            if (share.IsPaidByCaller && IsWithinTolerance(entryAmountMinor, share.ExpenseAmountMinor))
            {
                return PossibleDuplicateMatch.Whole;
            }

            return null;
        }

        private static bool IsWithinTolerance(long amountMinor, long referenceMinor)
        {
            return Math.Abs(amountMinor - referenceMinor) * WholePercent <= referenceMinor * AmountTolerancePercent;
        }
    }
}
