using Kvit.Domain.Entities;

namespace Kvit.Domain.RecurringIncomes
{
    public static class RecurringIncomeSchedule
    {
        private const int DaysInWeek = 7;

        public static IReadOnlyList<DateOnly> DueDates(RecurringIncomeFrequency frequency, int? dayOfMonth, int? dayOfWeek, DateOnly from, DateOnly to)
        {
            return frequency switch
            {
                RecurringIncomeFrequency.Monthly => MonthlyDueDates(dayOfMonth ?? throw new InvalidOperationException("A monthly repeating income has no day of the month."), from, to),
                RecurringIncomeFrequency.Weekly => WeeklyDueDates(dayOfWeek ?? throw new InvalidOperationException("A weekly repeating income has no weekday."), from, to),
                _ => throw new InvalidOperationException($"The repeating income frequency {frequency} has no schedule."),
            };
        }

        public static DateOnly FirstNewDate(RecurringIncomeFrequency frequency, DateOnly startsOn, DateOnly? lastEntryDate)
        {
            if (lastEntryDate is not DateOnly lastDate)
            {
                return startsOn;
            }

            return Later(startsOn, FirstDayAfterThePeriodOf(frequency, lastDate));
        }

        public static DateOnly? NextDate(RecurringIncomeFrequency frequency, int? dayOfMonth, int? dayOfWeek, DateOnly startsOn, DateOnly? endsOn, DateOnly? lastEntryDate, DateOnly today)
        {
            DateOnly from = Later(FirstNewDate(frequency, startsOn, lastEntryDate), today.AddDays(1));
            DateOnly lookAhead = from.AddMonths(1);
            DateOnly to = endsOn is DateOnly endDate && endDate < lookAhead ? endDate : lookAhead;
            IReadOnlyList<DateOnly> dates = DueDates(frequency, dayOfMonth, dayOfWeek, from, to);

            return dates.Count == 0 ? null : dates[0];
        }

        public static DateOnly Later(DateOnly first, DateOnly second)
        {
            return first > second ? first : second;
        }

        private static DateOnly FirstDayAfterThePeriodOf(RecurringIncomeFrequency frequency, DateOnly date)
        {
            return frequency switch
            {
                RecurringIncomeFrequency.Monthly => new DateOnly(date.Year, date.Month, 1).AddMonths(1),
                RecurringIncomeFrequency.Weekly => date.AddDays(DaysInWeek - (int)date.DayOfWeek),
                _ => throw new InvalidOperationException($"The repeating income frequency {frequency} has no period."),
            };
        }

        private static List<DateOnly> MonthlyDueDates(int dayOfMonth, DateOnly from, DateOnly to)
        {
            List<DateOnly> dates = [];
            for (DateOnly month = new(from.Year, from.Month, 1); month <= to; month = month.AddMonths(1))
            {
                DateOnly dueDate = new(month.Year, month.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(month.Year, month.Month)));
                if (dueDate >= from && dueDate <= to)
                {
                    dates.Add(dueDate);
                }
            }

            return dates;
        }

        private static List<DateOnly> WeeklyDueDates(int dayOfWeek, DateOnly from, DateOnly to)
        {
            List<DateOnly> dates = [];
            int daysUntilTheWeekday = (dayOfWeek - (int)from.DayOfWeek + DaysInWeek) % DaysInWeek;
            for (DateOnly dueDate = from.AddDays(daysUntilTheWeekday); dueDate <= to; dueDate = dueDate.AddDays(DaysInWeek))
            {
                dates.Add(dueDate);
            }

            return dates;
        }
    }
}
