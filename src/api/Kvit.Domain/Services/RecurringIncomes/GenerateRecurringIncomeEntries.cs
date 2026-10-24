using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Interfaces;
using Kvit.Domain.RecurringIncomes;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.RecurringIncomes
{
    public sealed class GenerateRecurringIncomeEntries(
        IRecurringIncomeRepository _incomes,
        IPersonalEntryRepository _entries,
        IExchangeRateRepository _exchangeRates,
        TimeProvider _timeProvider)
    {
        public async Task<Result<int>> Execute(Guid userId, string timeZoneId, CancellationToken cancellationToken)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateOnly today = UserLocalDate.Today(now, timeZoneId);
            DateOnly oldestAllowedDate = EntryAmountDateAndNote.FirstDayOfOldestMonth(today);
            IReadOnlyList<RecurringIncome> incomes = await _incomes.ListActiveOfInCreationOrderAsync(userId, cancellationToken);
            IReadOnlyDictionary<Guid, DateOnly> lastDates = await _entries.LastDatesFromRecurringIncomesOfAsync(userId, cancellationToken);

            List<(RecurringIncome Income, DateOnly EntryDate)> missing = [];
            foreach (RecurringIncome income in incomes)
            {
                DateOnly? lastDate = lastDates.TryGetValue(income.Id, out DateOnly last) ? last : null;
                DateOnly from = RecurringIncomeSchedule.Later(RecurringIncomeSchedule.FirstNewDate(income.Frequency, income.StartsOn, lastDate), oldestAllowedDate);
                DateOnly to = income.EndsOn is DateOnly endsOn && endsOn < today ? endsOn : today;
                missing.AddRange(RecurringIncomeSchedule.DueDates(income.Frequency, income.DayOfMonth, income.DayOfWeek, from, to).Select(entryDate => (income, entryDate)));
            }

            if (missing.Count == 0)
            {
                return Result.Ok(0);
            }

            StoredExchangeRates rates = await _exchangeRates.GetStoredCoveringAsync(missing.Min(due => due.EntryDate), missing.Max(due => due.EntryDate), cancellationToken);
            for (int position = 0; position < missing.Count; position++)
            {
                (RecurringIncome income, DateOnly entryDate) = missing[position];
                Guid idThatSortsInSavingOrder = Guid.CreateVersion7(now.AddMilliseconds(position));
                _entries.Add(PersonalEntry.CreateFromRecurringIncome(idThatSortsInSavingOrder, income, entryDate, rates.On(entryDate), now));
            }

            return Result.Ok(missing.Count);
        }
    }
}
