using Kvit.Api.Tests.Categories;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class CategoriesTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string BuiltInRows = "FROM categories WHERE owner_user_id IS NULL";

        [Theory]
        [InlineData("id", "uuid")]
        [InlineData("owner_user_id", "uuid")]
        [InlineData("key", "text")]
        [InlineData("name", "text")]
        [InlineData("emoji", "text")]
        [InlineData("color", "text")]
        [InlineData("sort_order", "integer")]
        [InlineData("archived_at", "timestamp with time zone")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "categories", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("id", "NO")]
        [InlineData("owner_user_id", "YES")]
        [InlineData("key", "YES")]
        [InlineData("name", "YES")]
        [InlineData("emoji", "NO")]
        [InlineData("color", "NO")]
        [InlineData("sort_order", "NO")]
        [InlineData("archived_at", "YES")]
        public async Task Column_HasTheExpectedNullability(string column, string expectedIsNullable)
        {
            string? isNullable = await SchemaQueries.IsNullableAsync(_database, "categories", column);

            Assert.Equal(expectedIsNullable, isNullable);
        }

        [Fact]
        public async Task Table_HasExactlyTheEightColumns()
        {
            List<string?> columns = await SchemaQueries.ColumnNamesAsync(_database, "categories");

            Assert.Equal(["archived_at", "color", "emoji", "id", "key", "name", "owner_user_id", "sort_order"], columns);
        }

        [Fact]
        public async Task Id_IsThePrimaryKey()
        {
            List<string?> primaryKeyColumns = await _database.QueryAsync(
                "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY (i.indkey) "
                + "WHERE i.indrelid = 'categories'::regclass AND i.indisprimary");

            Assert.Equal("id", Assert.Single(primaryKeyColumns));
        }

        [Fact]
        public async Task Seed_HasExactlyTenRowsAllBuiltIn()
        {
            List<string?> counts = await _database.QueryAsync("SELECT count(*)::text FROM categories");
            List<string?> builtInCounts = await _database.QueryAsync($"SELECT count(*)::text {BuiltInRows}");

            Assert.Equal("10", Assert.Single(counts));
            Assert.Equal("10", Assert.Single(builtInCounts));
        }

        [Fact]
        public async Task Seed_HasTheKeysInSortOrder()
        {
            List<string?> keys = await _database.QueryAsync($"SELECT key {BuiltInRows} ORDER BY sort_order");

            Assert.Equal(BuiltInCategories.Keys, keys);
        }

        [Fact]
        public async Task Seed_HasTheEmojiOfEachKeyInSortOrder()
        {
            List<string?> emojis = await _database.QueryAsync($"SELECT emoji {BuiltInRows} ORDER BY sort_order");

            Assert.Equal(BuiltInCategories.Emojis, emojis);
        }

        [Fact]
        public async Task Seed_HasSortOrderOneToTen()
        {
            List<string?> sortOrders = await _database.QueryAsync($"SELECT sort_order::text AS shown_sort_order {BuiltInRows} ORDER BY sort_order");

            Assert.Equal(["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"], sortOrders);
        }

        [Fact]
        public async Task Seed_HasANonEmptyDistinctColorNameForEveryRow()
        {
            List<string?> colors = await _database.QueryAsync($"SELECT color {BuiltInRows} ORDER BY sort_order");

            Assert.All(colors, color => Assert.False(string.IsNullOrWhiteSpace(color)));
            Assert.Equal(10, colors.Distinct(StringComparer.Ordinal).Count());
            Assert.All(colors, color => Assert.DoesNotMatch("^#", color!));
        }

        [Fact]
        public async Task Seed_HasNoNameAndNoArchiveDateOnAnyRow()
        {
            List<string?> withNameOrArchive = await _database.QueryAsync("SELECT id::text FROM categories WHERE name IS NOT NULL OR archived_at IS NOT NULL");

            Assert.Empty(withNameOrArchive);
        }

        [Fact]
        public async Task Seed_HasTenDistinctIds()
        {
            List<string?> ids = await _database.QueryAsync("SELECT id::text FROM categories");

            Assert.Equal(10, ids.Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public async Task Migration_CategoriesAndExchangeRates_IsInTheMigrationHistory()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            IEnumerable<string> applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

            Assert.Single(applied, migration => migration.EndsWith("_CategoriesAndExchangeRates", StringComparison.Ordinal));
        }
    }
}
