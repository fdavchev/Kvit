using System.Globalization;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public class EntryAmountDateAndNote
    {
        public const int AllowedMonths = 24;
        public const int TimeZoneToleranceDays = 1;

        protected EntryAmountDateAndNote(EntryAmountDateAndNote checkedFields)
            : this(checkedFields.Amount, checkedFields.EntryDate, checkedFields.Note)
        {
        }

        private EntryAmountDateAndNote(Money amount, DateOnly entryDate, string? note)
        {
            Amount = amount;
            EntryDate = entryDate;
            Note = note;
        }

        public Money Amount { get; }

        public DateOnly EntryDate { get; }

        public string? Note { get; }

        public static Result<EntryAmountDateAndNote> Check(IncomeInput input, DateOnly utcToday)
        {
            return Check(input, utcToday, null);
        }

        public static Result<EntryAmountDateAndNote> CheckForEdit(IncomeInput input, DateOnly utcToday, DateOnly currentEntryDate)
        {
            return Check(input, utcToday, currentEntryDate);
        }

        public static DateOnly EarliestDateOn(DateOnly utcToday)
        {
            return FirstDayOfOldestMonth(utcToday).AddDays(-TimeZoneToleranceDays);
        }

        public static DateOnly FirstDayOfOldestMonth(DateOnly today)
        {
            DateOnly firstDayOfMonth = new(today.Year, today.Month, 1);

            return firstDayOfMonth.AddMonths(-(AllowedMonths - 1));
        }

        private static Result<EntryAmountDateAndNote> Check(IncomeInput input, DateOnly utcToday, DateOnly? currentEntryDate)
        {
            Result<Currency> currency = ExpenseFields.CheckCurrency(input.Currency, ResultCodes.PERSONAL_ENTRY_CURRENCY_INVALID);
            if (!currency.IsSuccess)
            {
                return currency.ToFailure<EntryAmountDateAndNote>();
            }

            Result<Money> amount = ExpenseFields.CheckAmount(input.AmountMinor, currency.Value, ResultCodes.PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE, ResultCodes.PERSONAL_ENTRY_AMOUNT_TOO_LARGE);
            if (!amount.IsSuccess)
            {
                return amount.ToFailure<EntryAmountDateAndNote>();
            }

            Result<DateOnly> entryDate = CheckEntryDate(input.EntryDate, utcToday, currentEntryDate);
            if (!entryDate.IsSuccess)
            {
                return entryDate.ToFailure<EntryAmountDateAndNote>();
            }

            Result<string?> note = ExpenseFields.CheckNote(input.Note, ResultCodes.PERSONAL_ENTRY_NOTE_INVALID);
            if (!note.IsSuccess)
            {
                return note.ToFailure<EntryAmountDateAndNote>();
            }

            return Result.Ok(new EntryAmountDateAndNote(amount.Value, entryDate.Value, note.Value));
        }

        private static Result<DateOnly> CheckEntryDate(string dateText, DateOnly utcToday, DateOnly? currentEntryDate)
        {
            if (currentEntryDate is DateOnly keptDate && dateText == keptDate.ToString(ExpenseFields.DateFormat, CultureInfo.InvariantCulture))
            {
                return Result.Ok(keptDate);
            }

            DateOnly latestDate = utcToday.AddDays(TimeZoneToleranceDays);

            return ExpenseFields.CheckDate(dateText, EarliestDateOn(utcToday), latestDate, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
        }
    }
}
