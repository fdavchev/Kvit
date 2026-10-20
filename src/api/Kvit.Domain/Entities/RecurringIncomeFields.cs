using System.Globalization;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public sealed class RecurringIncomeFields
    {
        public const int TitleMaxLength = 60;
        public const int StartMaxYearsAhead = 1;
        public const int EndMaxYearsAfterStart = 10;

        private RecurringIncomeFields(Money amount, string? title, RecurringIncomeFrequency frequency, int? dayOfMonth, int? dayOfWeek, DateOnly startsOn, DateOnly? endsOn)
        {
            Amount = amount;
            Title = title;
            Frequency = frequency;
            DayOfMonth = dayOfMonth;
            DayOfWeek = dayOfWeek;
            StartsOn = startsOn;
            EndsOn = endsOn;
        }

        public Money Amount { get; }

        public string? Title { get; }

        public RecurringIncomeFrequency Frequency { get; }

        public int? DayOfMonth { get; }

        public int? DayOfWeek { get; }

        public DateOnly StartsOn { get; }

        public DateOnly? EndsOn { get; }

        public static Result<RecurringIncomeFields> Create(RecurringIncomeInput input, DateOnly utcToday)
        {
            return Create(input, startsOnText => CheckStartDate(startsOnText, utcToday));
        }

        public static Result<RecurringIncomeFields> CreateForEdit(RecurringIncomeInput input, DateOnly utcToday, DateOnly currentStartsOn, bool hasEntries)
        {
            return Create(input, startsOnText => CheckChangedStartDate(startsOnText, utcToday, currentStartsOn, hasEntries));
        }

        private static Result<RecurringIncomeFields> Create(RecurringIncomeInput input, Func<string, Result<DateOnly>> checkStartDate)
        {
            Result<Currency> currency = ExpenseFields.CheckCurrency(input.Currency, ResultCodes.RECURRING_INCOME_CURRENCY_INVALID);
            if (!currency.IsSuccess)
            {
                return currency.ToFailure<RecurringIncomeFields>();
            }

            Result<Money> amount = ExpenseFields.CheckAmount(input.AmountMinor, currency.Value, ResultCodes.RECURRING_INCOME_AMOUNT_NOT_POSITIVE, ResultCodes.RECURRING_INCOME_AMOUNT_TOO_LARGE);
            if (!amount.IsSuccess)
            {
                return amount.ToFailure<RecurringIncomeFields>();
            }

            Result<string?> title = ExpenseFields.CheckTitle(input.Title, TitleMaxLength, ResultCodes.RECURRING_INCOME_TITLE_INVALID);
            if (!title.IsSuccess)
            {
                return title.ToFailure<RecurringIncomeFields>();
            }

            Result<RecurringIncomeFrequency> frequency = CheckFrequency(input.Frequency);
            if (!frequency.IsSuccess)
            {
                return frequency.ToFailure<RecurringIncomeFields>();
            }

            Result day = CheckDay(frequency.Value, input.DayOfMonth, input.DayOfWeek);
            if (!day.IsSuccess)
            {
                return day.ToFailure<RecurringIncomeFields>();
            }

            Result<DateOnly> startsOn = checkStartDate(input.StartsOn);
            if (!startsOn.IsSuccess)
            {
                return startsOn.ToFailure<RecurringIncomeFields>();
            }

            Result<DateOnly?> endsOn = CheckEndDate(input.EndsOn, startsOn.Value);
            if (!endsOn.IsSuccess)
            {
                return endsOn.ToFailure<RecurringIncomeFields>();
            }

            return Result.Ok(new RecurringIncomeFields(amount.Value, title.Value, frequency.Value, input.DayOfMonth, input.DayOfWeek, startsOn.Value, endsOn.Value));
        }

        private static Result<RecurringIncomeFrequency> CheckFrequency(string frequency)
        {
            string[] frequencyNames = Enum.GetNames<RecurringIncomeFrequency>();
            if (!frequencyNames.Contains(frequency, StringComparer.Ordinal))
            {
                return Result.Failure<RecurringIncomeFrequency>($"'{frequency}' is not a frequency. Use one of: {string.Join(", ", frequencyNames)}.", ResultCodes.RECURRING_INCOME_FREQUENCY_INVALID);
            }

            return Result.Ok(Enum.Parse<RecurringIncomeFrequency>(frequency));
        }

        private static Result CheckDay(RecurringIncomeFrequency frequency, int? dayOfMonth, int? dayOfWeek)
        {
            return frequency switch
            {
                RecurringIncomeFrequency.Monthly when dayOfWeek is not null || dayOfMonth is not (>= 1 and <= RecurringIncome.LastDayOfMonth) =>
                    Result.Failure($"A monthly income needs a day of the month from 1 to {RecurringIncome.LastDayOfMonth} and no weekday; got day of month {dayOfMonth?.ToString(CultureInfo.InvariantCulture) ?? "none"} and weekday {dayOfWeek?.ToString(CultureInfo.InvariantCulture) ?? "none"}.", ResultCodes.RECURRING_INCOME_DAY_INVALID),
                RecurringIncomeFrequency.Weekly when dayOfMonth is not null || dayOfWeek is not (>= 0 and <= RecurringIncome.LastDayOfWeek) =>
                    Result.Failure($"A weekly income needs a weekday from 0 to {RecurringIncome.LastDayOfWeek} and no day of the month; got weekday {dayOfWeek?.ToString(CultureInfo.InvariantCulture) ?? "none"} and day of month {dayOfMonth?.ToString(CultureInfo.InvariantCulture) ?? "none"}.", ResultCodes.RECURRING_INCOME_DAY_INVALID),
                _ => Result.Ok(),
            };
        }

        private static Result<DateOnly> CheckStartDate(string startsOnText, DateOnly utcToday)
        {
            return ExpenseFields.CheckDate(startsOnText, IncomeFields.EarliestDateOn(utcToday), utcToday.AddYears(StartMaxYearsAhead), ResultCodes.RECURRING_INCOME_START_DATE_INVALID);
        }

        private static Result<DateOnly> CheckChangedStartDate(string startsOnText, DateOnly utcToday, DateOnly currentStartsOn, bool hasEntries)
        {
            if (startsOnText == currentStartsOn.ToString(ExpenseFields.DateFormat, CultureInfo.InvariantCulture))
            {
                return Result.Ok(currentStartsOn);
            }

            if (hasEntries)
            {
                return Result.Failure<DateOnly>($"The start date cannot change from {currentStartsOn.ToString(ExpenseFields.DateFormat, CultureInfo.InvariantCulture)} to '{startsOnText}' because entries were already created from this income.", ResultCodes.RECURRING_INCOME_START_CANNOT_CHANGE);
            }

            return CheckStartDate(startsOnText, utcToday);
        }

        private static Result<DateOnly?> CheckEndDate(string? endsOnText, DateOnly startsOn)
        {
            if (endsOnText is null)
            {
                return Result.Ok<DateOnly?>(null);
            }

            Result<DateOnly> endsOn = ExpenseFields.CheckDate(endsOnText, startsOn, startsOn.AddYears(EndMaxYearsAfterStart), ResultCodes.RECURRING_INCOME_END_DATE_INVALID);
            if (!endsOn.IsSuccess)
            {
                return endsOn.ToFailure<DateOnly?>();
            }

            return Result.Ok<DateOnly?>(endsOn.Value);
        }
    }
}
