namespace Kvit.Api.Tests.RecurringIncomes
{
    public sealed record RecurringIncomeBody(
        long AmountMinor,
        string Currency,
        string? Title,
        string Frequency,
        int? DayOfMonth,
        int? DayOfWeek,
        string StartsOn,
        string? EndsOn)
    {
        public const long Salary = 4_500_000;
        public const string Mkd = "MKD";
        public const string Eur = "EUR";
        public const string DefaultStart = "2026-10-01";

        public static RecurringIncomeBody Monthly(
            long amountMinor = Salary,
            string currency = Mkd,
            string? title = "Salary",
            int? dayOfMonth = 15,
            string startsOn = DefaultStart,
            string? endsOn = null)
        {
            return new RecurringIncomeBody(amountMinor, currency, title, "Monthly", dayOfMonth, null, startsOn, endsOn);
        }

        public static RecurringIncomeBody Weekly(
            long amountMinor = Salary,
            string currency = Mkd,
            string? title = "Salary",
            int? dayOfWeek = 3,
            string startsOn = DefaultStart,
            string? endsOn = null)
        {
            return new RecurringIncomeBody(amountMinor, currency, title, "Weekly", null, dayOfWeek, startsOn, endsOn);
        }

        public static RecurringIncomeBody Of(
            string frequency,
            int? dayOfMonth,
            int? dayOfWeek,
            long amountMinor = Salary,
            string currency = Mkd,
            string? title = "Salary",
            string startsOn = DefaultStart,
            string? endsOn = null)
        {
            return new RecurringIncomeBody(amountMinor, currency, title, frequency, dayOfMonth, dayOfWeek, startsOn, endsOn);
        }
    }
}
