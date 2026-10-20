using System.Globalization;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public sealed class ExpenseFields
    {
        public const int TitleMaxLength = 80;
        public const int NoteMaxLength = 500;
        public const long AmountMaxMinorUnits = 999_999_999_999;
        public const int DateMaxDaysAhead = 366;
        public const string DateFormat = "yyyy-MM-dd";

        public static readonly DateOnly EarliestDate = new(2000, 1, 1);

        private ExpenseFields(string? title, string? note, Money amount, DateOnly expenseDate, Guid? categoryId, SplitType splitType)
        {
            Title = title;
            Note = note;
            Amount = amount;
            ExpenseDate = expenseDate;
            CategoryId = categoryId;
            SplitType = splitType;
        }

        public string? Title { get; }

        public string? Note { get; }

        public Money Amount { get; }

        public DateOnly ExpenseDate { get; }

        public Guid? CategoryId { get; }

        public SplitType SplitType { get; }

        public static Result<ExpenseFields> Create(
            string? title,
            string? note,
            long amountMinor,
            string currency,
            string expenseDate,
            Guid? categoryId,
            string splitType,
            DateOnly utcToday,
            int titleMaxLength)
        {
            Result<Currency> checkedCurrency = CheckCurrency(currency, ResultCodes.EXPENSE_CURRENCY_INVALID);
            if (!checkedCurrency.IsSuccess)
            {
                return checkedCurrency.ToFailure<ExpenseFields>();
            }

            string[] splitTypeNames = Enum.GetNames<SplitType>();
            if (!splitTypeNames.Contains(splitType, StringComparer.Ordinal))
            {
                return Result.Failure<ExpenseFields>($"'{splitType}' is not a split type. Use one of: {string.Join(", ", splitTypeNames)}.", ResultCodes.EXPENSE_SPLIT_TYPE_INVALID);
            }

            Result<Money> amount = CheckAmount(amountMinor, checkedCurrency.Value, ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE, ResultCodes.EXPENSE_AMOUNT_TOO_LARGE);
            if (!amount.IsSuccess)
            {
                return amount.ToFailure<ExpenseFields>();
            }

            Result<DateOnly> date = CheckDate(expenseDate, utcToday, ResultCodes.EXPENSE_DATE_INVALID);
            if (!date.IsSuccess)
            {
                return date.ToFailure<ExpenseFields>();
            }

            string? trimmedTitle = TrimToNull(title);
            if (trimmedTitle is not null && trimmedTitle.Length > titleMaxLength)
            {
                return Result.Failure<ExpenseFields>($"The title must have at most {titleMaxLength} characters; it has {trimmedTitle.Length}.", ResultCodes.EXPENSE_TITLE_INVALID);
            }

            Result<string?> trimmedNote = CheckNote(note, ResultCodes.EXPENSE_NOTE_INVALID);
            if (!trimmedNote.IsSuccess)
            {
                return trimmedNote.ToFailure<ExpenseFields>();
            }

            return Result.Ok(new ExpenseFields(trimmedTitle, trimmedNote.Value, amount.Value, date.Value, categoryId, Enum.Parse<SplitType>(splitType)));
        }

        public static string? TrimToNull(string? text)
        {
            string trimmed = text?.Trim() ?? string.Empty;

            return trimmed.Length == 0 ? null : trimmed;
        }

        public static Result<Currency> CheckCurrency(string currency, string invalidCurrencyCode)
        {
            string[] currencyNames = Enum.GetNames<Currency>();
            if (!currencyNames.Contains(currency, StringComparer.Ordinal))
            {
                return Result.Failure<Currency>($"'{currency}' is not a supported currency. Use one of: {string.Join(", ", currencyNames)}.", invalidCurrencyCode);
            }

            return Result.Ok(Enum.Parse<Currency>(currency));
        }

        public static Result<Money> CheckAmount(long amountMinor, Currency currency, string notPositiveCode, string tooLargeCode)
        {
            if (amountMinor <= 0)
            {
                return Result.Failure<Money>($"The amount must be more than 0; got {amountMinor} minor units.", notPositiveCode);
            }

            if (amountMinor > AmountMaxMinorUnits)
            {
                return Result.Failure<Money>($"The amount must be at most {AmountMaxMinorUnits} minor units; got {amountMinor}.", tooLargeCode);
            }

            return Money.Create(amountMinor, currency);
        }

        public static Result<DateOnly> CheckDate(string dateText, DateOnly utcToday, string invalidDateCode)
        {
            return CheckDate(dateText, EarliestDate, utcToday.AddDays(DateMaxDaysAhead), invalidDateCode);
        }

        public static Result<DateOnly> CheckDate(string dateText, DateOnly earliestDate, DateOnly latestDate, string invalidDateCode)
        {
            string allowedRange = $"from {earliestDate.ToString(DateFormat, CultureInfo.InvariantCulture)} to {latestDate.ToString(DateFormat, CultureInfo.InvariantCulture)}";
            if (!DateOnly.TryParseExact(dateText, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            {
                return Result.Failure<DateOnly>($"'{dateText}' is not a date in the form {DateFormat}; pick a date {allowedRange}.", invalidDateCode);
            }

            if (date < earliestDate || date > latestDate)
            {
                return Result.Failure<DateOnly>($"The date {dateText} is outside the allowed range {allowedRange}.", invalidDateCode);
            }

            return Result.Ok(date);
        }

        public static Result<string?> CheckNote(string? note, string invalidNoteCode)
        {
            string? trimmedNote = TrimToNull(note);
            if (trimmedNote is not null && trimmedNote.Length > NoteMaxLength)
            {
                return Result.Failure<string?>($"The note must have at most {NoteMaxLength} characters; it has {trimmedNote.Length}.", invalidNoteCode);
            }

            return Result.Ok(trimmedNote);
        }
    }
}
