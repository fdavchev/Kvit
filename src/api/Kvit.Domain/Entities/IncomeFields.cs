using System.Globalization;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public sealed class IncomeFields
    {
        public const int AllowedMonths = 24;
        public const int TimeZoneToleranceDays = 1;

        private IncomeFields(Money amount, DateOnly entryDate, string? note)
        {
            Amount = amount;
            EntryDate = entryDate;
            Note = note;
        }

        public Money Amount { get; }

        public DateOnly EntryDate { get; }

        public string? Note { get; }

        public static Result<IncomeFields> Create(IncomeInput input, DateOnly utcToday)
        {
            return Create(input, utcToday, null);
        }

        public static Result<IncomeFields> CreateForEdit(IncomeInput input, DateOnly utcToday, DateOnly currentEntryDate)
        {
            return Create(input, utcToday, currentEntryDate);
        }

        public static DateOnly EarliestDateOn(DateOnly utcToday)
        {
            DateOnly firstDayOfUtcMonth = new(utcToday.Year, utcToday.Month, 1);

            return firstDayOfUtcMonth.AddMonths(-(AllowedMonths - 1)).AddDays(-TimeZoneToleranceDays);
        }

        private static Result<IncomeFields> Create(IncomeInput input, DateOnly utcToday, DateOnly? currentEntryDate)
        {
            Result<Currency> currency = ExpenseFields.CheckCurrency(input.Currency, ResultCodes.PERSONAL_ENTRY_CURRENCY_INVALID);
            if (!currency.IsSuccess)
            {
                return currency.ToFailure<IncomeFields>();
            }

            Result<Money> amount = ExpenseFields.CheckAmount(input.AmountMinor, currency.Value, ResultCodes.PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE, ResultCodes.PERSONAL_ENTRY_AMOUNT_TOO_LARGE);
            if (!amount.IsSuccess)
            {
                return amount.ToFailure<IncomeFields>();
            }

            Result<DateOnly> entryDate = CheckEntryDate(input.EntryDate, utcToday, currentEntryDate);
            if (!entryDate.IsSuccess)
            {
                return entryDate.ToFailure<IncomeFields>();
            }

            Result<string?> note = ExpenseFields.CheckNote(input.Note, ResultCodes.PERSONAL_ENTRY_NOTE_INVALID);
            if (!note.IsSuccess)
            {
                return note.ToFailure<IncomeFields>();
            }

            return Result.Ok(new IncomeFields(amount.Value, entryDate.Value, note.Value));
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
