using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class ExchangeRatesTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Theory]
        [InlineData("rate_date", "date")]
        [InlineData("mkd_per_eur", "numeric")]
        [InlineData("fetched_at", "timestamp with time zone")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "exchange_rates", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("rate_date", "NO")]
        [InlineData("mkd_per_eur", "NO")]
        [InlineData("fetched_at", "NO")]
        public async Task Column_IsNotNullable(string column, string expectedIsNullable)
        {
            string? isNullable = await SchemaQueries.IsNullableAsync(_database, "exchange_rates", column);

            Assert.Equal(expectedIsNullable, isNullable);
        }

        [Fact]
        public async Task Table_HasExactlyTheThreeColumns()
        {
            List<string?> columns = await SchemaQueries.ColumnNamesAsync(_database, "exchange_rates");

            Assert.Equal(["fetched_at", "mkd_per_eur", "rate_date"], columns);
        }

        [Fact]
        public async Task MkdPerEur_IsNumeric10WithFourDecimals()
        {
            List<string?> precisionAndScale = await _database.QueryAsync(
                "SELECT numeric_precision || ',' || numeric_scale FROM information_schema.columns "
                + "WHERE table_schema = 'public' AND table_name = 'exchange_rates' AND column_name = 'mkd_per_eur'");

            Assert.Equal("10,4", Assert.Single(precisionAndScale));
        }

        [Fact]
        public async Task RateDate_IsThePrimaryKey()
        {
            List<string?> primaryKeyColumns = await _database.QueryAsync(
                "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY (i.indkey) "
                + "WHERE i.indrelid = 'exchange_rates'::regclass AND i.indisprimary");

            Assert.Equal("rate_date", Assert.Single(primaryKeyColumns));
        }

        [Fact]
        public async Task RateDate_UsedTwice_ViolatesThePrimaryKey()
        {
            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync(
                    "INSERT INTO exchange_rates (rate_date, mkd_per_eur, fetched_at) VALUES ('2026-09-24', 61.7000, now())"),
                PostgresErrorCodes.UniqueViolation);
        }

        [Fact]
        public async Task Seed_HasExactlyOneRow()
        {
            List<string?> counts = await _database.QueryAsync("SELECT count(*)::text FROM exchange_rates");

            Assert.Equal("1", Assert.Single(counts));
        }

        [Fact]
        public async Task Seed_IsTheRealNbrmRate61Point5610For2026September24()
        {
            List<string?> dates = await _database.QueryAsync("SELECT rate_date::text FROM exchange_rates");
            List<string?> rates = await _database.QueryAsync("SELECT mkd_per_eur::text FROM exchange_rates");

            Assert.Equal("2026-09-24", Assert.Single(dates));
            Assert.Equal("61.5610", Assert.Single(rates));
        }

        [Fact]
        public async Task Seed_HasAFetchedAtValue()
        {
            List<string?> fetchedAt = await _database.QueryAsync("SELECT fetched_at::text FROM exchange_rates WHERE rate_date = @date", new NpgsqlParameter("date", new DateOnly(2026, 9, 24)));

            Assert.False(string.IsNullOrWhiteSpace(Assert.Single(fetchedAt)));
        }

        [Fact]
        public async Task GetLatestAsync_OnTheSeededDatabase_ReturnsTheSeededRate()
        {
            using IServiceScope scope = _database.CreateScope();
            IExchangeRateRepository repository = scope.ServiceProvider.GetRequiredService<IExchangeRateRepository>();

            ExchangeRate latest = await repository.GetLatestAsync(TestContext.Current.CancellationToken);

            Assert.Equal(new DateOnly(2026, 9, 24), latest.RateDate);
            Assert.Equal(61.5610m, latest.MkdPerEur);
        }
    }
}
