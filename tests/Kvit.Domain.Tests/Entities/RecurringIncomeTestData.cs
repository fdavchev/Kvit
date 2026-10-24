using Kvit.Domain.Entities;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Tests.Entities
{
    public static class RecurringIncomeTestData
    {
        public const long Salary = 4_500_000;
        public const string Mkd = "MKD";
        public const string Monthly = "Monthly";
        public const string Weekly = "Weekly";
        public const string StartsOn = "2026-10-01";

        public static readonly Guid UserId = PersonalEntryTestData.UserId;
        public static readonly DateTimeOffset Moment = PersonalEntryTestData.Moment;
        public static readonly DateOnly UtcToday = PersonalEntryTestData.UtcToday;

        public static RecurringIncomeInput MonthlyInput(
            long amountMinor = Salary,
            string currency = Mkd,
            string? title = "Salary",
            int? dayOfMonth = 15,
            string startsOn = StartsOn,
            string? endsOn = null)
        {
            return new RecurringIncomeInput(amountMinor, currency, title, Monthly, dayOfMonth, null, startsOn, endsOn);
        }

        public static RecurringIncomeInput WeeklyInput(
            long amountMinor = Salary,
            string currency = Mkd,
            string? title = "Salary",
            int? dayOfWeek = 3,
            string startsOn = StartsOn,
            string? endsOn = null)
        {
            return new RecurringIncomeInput(amountMinor, currency, title, Weekly, null, dayOfWeek, startsOn, endsOn);
        }

        public static RecurringIncomeInput InputOf(
            string frequency,
            int? dayOfMonth,
            int? dayOfWeek,
            long amountMinor = Salary,
            string currency = Mkd,
            string? title = "Salary",
            string startsOn = StartsOn,
            string? endsOn = null)
        {
            return new RecurringIncomeInput(amountMinor, currency, title, frequency, dayOfMonth, dayOfWeek, startsOn, endsOn);
        }

        public static Result<RecurringIncome> Create(RecurringIncomeInput input)
        {
            return RecurringIncome.Create(UserId, input, Guid.CreateVersion7(), UtcToday, Moment);
        }

        public static Result<RecurringIncome> CreateOn(RecurringIncomeInput input, DateOnly utcToday)
        {
            return RecurringIncome.Create(UserId, input, Guid.CreateVersion7(), utcToday, Moment);
        }

        public static RecurringIncome NewMonthly(RecurringIncomeInput? input = null)
        {
            return Create(input ?? MonthlyInput()).Value;
        }

        public static RecurringIncome NewWeekly(RecurringIncomeInput? input = null)
        {
            return Create(input ?? WeeklyInput()).Value;
        }
    }
}
