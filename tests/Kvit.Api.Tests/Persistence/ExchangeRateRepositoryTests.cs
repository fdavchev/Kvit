using System.Globalization;
using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class ExchangeRateRepositoryTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private static readonly DateTimeOffset FirstFetch = new(2026, 10, 5, 8, 15, 30, TimeSpan.Zero);
        private static readonly DateTimeOffset SecondFetch = new(2026, 10, 5, 17, 45, 10, TimeSpan.Zero);

        [Fact]
        public async Task SaveFetchedAsync_NewRateDate_InsertsOneRowWithTheGivenValues()
        {
            DateOnly rateDate = new(2031, 3, 3);

            await SaveAsync(rateDate, 61.6333m, FirstFetch);

            Assert.Equal(1, await CountRowsAsync(rateDate));
            Assert.Equal("61.6333", await RateTextAsync(rateDate));
            Assert.Equal("2026-10-05T08:15:30", await FetchedAtTextAsync(rateDate));
        }

        [Fact]
        public async Task SaveFetchedAsync_RateDateAlreadyStored_UpdatesTheSameRowInsteadOfAddingASecondOne()
        {
            DateOnly rateDate = new(2031, 4, 4);
            await SaveAsync(rateDate, 61.5000m, FirstFetch);

            await SaveAsync(rateDate, 61.7500m, SecondFetch);

            Assert.Equal(1, await CountRowsAsync(rateDate));
            Assert.Equal("61.7500", await RateTextAsync(rateDate));
            Assert.Equal("2026-10-05T17:45:10", await FetchedAtTextAsync(rateDate));
        }

        [Fact]
        public async Task SaveFetchedAsync_SameRateDateWithTheSameRate_OnlyMovesFetchedAt()
        {
            DateOnly rateDate = new(2031, 5, 5);
            await SaveAsync(rateDate, 61.5610m, FirstFetch);

            await SaveAsync(rateDate, 61.5610m, SecondFetch);

            Assert.Equal(1, await CountRowsAsync(rateDate));
            Assert.Equal("61.5610", await RateTextAsync(rateDate));
            Assert.Equal("2026-10-05T17:45:10", await FetchedAtTextAsync(rateDate));
        }

        [Fact]
        public async Task SaveFetchedAsync_AnotherRateDate_LeavesTheOtherRowsUntouched()
        {
            DateOnly first = new(2031, 6, 6);
            DateOnly second = new(2031, 6, 7);
            await SaveAsync(first, 61.1111m, FirstFetch);

            await SaveAsync(second, 61.2222m, SecondFetch);

            Assert.Equal("61.1111", await RateTextAsync(first));
            Assert.Equal("2026-10-05T08:15:30", await FetchedAtTextAsync(first));
            Assert.Equal("61.2222", await RateTextAsync(second));
        }

        [Fact]
        public async Task GetLatestAsync_SeveralRateDates_ReturnsTheRowWithTheNewestRateDateWhateverTheSaveOrder()
        {
            DateOnly newest = new(2032, 2, 2);
            await SaveAsync(newest, 62.2222m, FirstFetch);
            await SaveAsync(new DateOnly(2032, 1, 1), 62.1111m, SecondFetch);

            using IServiceScope scope = _database.CreateScope();
            IExchangeRateRepository repository = scope.ServiceProvider.GetRequiredService<IExchangeRateRepository>();
            ExchangeRate latest = await repository.GetLatestAsync(TestContext.Current.CancellationToken);

            Assert.Equal(newest, latest.RateDate);
            Assert.Equal(62.2222m, latest.MkdPerEur);
            Assert.Equal(FirstFetch, latest.FetchedAt);
        }

        private async Task SaveAsync(DateOnly rateDate, decimal mkdPerEur, DateTimeOffset fetchedAt)
        {
            using IServiceScope scope = _database.CreateScope();
            IExchangeRateRepository repository = scope.ServiceProvider.GetRequiredService<IExchangeRateRepository>();

            await repository.SaveFetchedAsync(rateDate, mkdPerEur, fetchedAt, TestContext.Current.CancellationToken);
        }

        private async Task<int> CountRowsAsync(DateOnly rateDate)
        {
            List<string?> counts = await _database.QueryAsync(
                "SELECT count(*)::text FROM exchange_rates WHERE rate_date = @date",
                new NpgsqlParameter("date", rateDate));

            return int.Parse(Assert.Single(counts) ?? throw new InvalidOperationException("count(*) answered null."), CultureInfo.InvariantCulture);
        }

        private async Task<string?> RateTextAsync(DateOnly rateDate)
        {
            List<string?> rates = await _database.QueryAsync(
                "SELECT mkd_per_eur::text FROM exchange_rates WHERE rate_date = @date",
                new NpgsqlParameter("date", rateDate));

            return Assert.Single(rates);
        }

        private async Task<string?> FetchedAtTextAsync(DateOnly rateDate)
        {
            List<string?> fetchedAt = await _database.QueryAsync(
                "SELECT to_char(fetched_at AT TIME ZONE 'UTC', 'YYYY-MM-DD\"T\"HH24:MI:SS') FROM exchange_rates WHERE rate_date = @date",
                new NpgsqlParameter("date", rateDate));

            return Assert.Single(fetchedAt);
        }
    }
}
