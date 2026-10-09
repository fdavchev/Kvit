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
            string[] currencyNames = Enum.GetNames<Currency>();
            if (!currencyNames.Contains(currency, StringComparer.Ordinal))
            {
                return Result.Failure<ExpenseFields>($"'{currency}' is not a supported currency. Use one of: {string.Join(", ", currencyNames)}.", ResultCodes.EXPENSE_CURRENCY_INVALID);
            }

            string[] splitTypeNames = Enum.GetNames<SplitType>();
            if (!splitTypeNames.Contains(splitType, StringComparer.Ordinal))
            {
                return Result.Failure<ExpenseFields>($"'{splitType}' is not a split type. Use one of: {string.Join(", ", splitTypeNames)}.", ResultCodes.EXPENSE_SPLIT_TYPE_INVALID);
            }

            Result<Money> amount = CheckAmount(amountMinor, Enum.Parse<Currency>(currency));
            if (!amount.IsSuccess)
            {
                return amount.ToFailure<ExpenseFields>();
            }

            Result<DateOnly> date = CheckDate(expenseDate, utcToday);
            if (!date.IsSuccess)
            {
                return date.ToFailure<ExpenseFields>();
            }

            string? trimmedTitle = TrimToNull(title);
            if (trimmedTitle is not null && trimmedTitle.Length > titleMaxLength)
            {
                return Result.Failure<ExpenseFields>($"The title must have at most {titleMaxLength} characters; it has {trimmedTitle.Length}.", ResultCodes.EXPENSE_TITLE_INVALID);
            }

            string? trimmedNote = TrimToNull(note);
            if (trimmedNote is not null && trimmedNote.Length > NoteMaxLength)
            {
                return Result.Failure<ExpenseFields>($"The note must have at most {NoteMaxLength} characters; it has {trimmedNote.Length}.", ResultCodes.EXPENSE_NOTE_INVALID);
            }

            return Result.Ok(new ExpenseFields(trimmedTitle, trimmedNote, amount.Value, date.Value, categoryId, Enum.Parse<SplitType>(splitType)));
        }

        public static string? TrimToNull(string? text)
        {
            string trimmed = text?.Trim() ?? string.Empty;

            return trimmed.Length == 0 ? null : trimmed;
        }

        private static Result<Money> CheckAmount(long amountMinor, Currency currency)
        {
            if (amountMinor <= 0)
            {
                return Result.Failure<Money>($"The expense amount must be more than 0; got {amountMinor} minor units.", ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE);
            }

            if (amountMinor > AmountMaxMinorUnits)
            {
                return Result.Failure<Money>($"The expense amount must be at most {AmountMaxMinorUnits} minor units; got {amountMinor}.", ResultCodes.EXPENSE_AMOUNT_TOO_LARGE);
            }

            return Money.Create(amountMinor, currency);
        }

        private static Result<DateOnly> CheckDate(string expenseDate, DateOnly utcToday)
        {
            DateOnly latestDate = utcToday.AddDays(DateMaxDaysAhead);
            string allowedRange = $"from {EarliestDate.ToString(DateFormat, CultureInfo.InvariantCulture)} to {latestDate.ToString(DateFormat, CultureInfo.InvariantCulture)}";
            if (!DateOnly.TryParseExact(expenseDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            {
                return Result.Failure<DateOnly>($"'{expenseDate}' is not a date in the form {DateFormat}; pick a date {allowedRange}.", ResultCodes.EXPENSE_DATE_INVALID);
            }

            if (date < EarliestDate || date > latestDate)
            {
                return Result.Failure<DateOnly>($"The expense date {expenseDate} is outside the allowed range {allowedRange}.", ResultCodes.EXPENSE_DATE_INVALID);
            }

            return Result.Ok(date);
        }
    }
}
