using Kvit.Domain.Entities;

namespace Kvit.Domain.Tests.Services.ExchangeRates
{
    public static class ExchangeRateTestData
    {
        public static readonly DateTimeOffset Now = new(2026, 10, 5, 23, 30, 0, TimeSpan.Zero);
        public static readonly DateOnly UtcToday = new(2026, 10, 5);
        public static readonly DateOnly LastFriday = new(2026, 10, 2);
        public static readonly DateOnly SeededDate = new(2026, 9, 24);
        public const decimal SeededRate = 61.5610m;
        public const decimal FetchedRate = 61.6333m;

        public static ExchangeRate Row(DateOnly rateDate, decimal mkdPerEur, DateTimeOffset fetchedAt)
        {
            return ExchangeRate.Create(rateDate, mkdPerEur, fetchedAt).Value;
        }
    }
}
