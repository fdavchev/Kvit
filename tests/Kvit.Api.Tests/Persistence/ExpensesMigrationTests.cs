using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class ExpensesMigrationTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Fact]
        public async Task Migration_Expenses_IsInTheMigrationHistoryExactlyOnce()
        {
            List<string> applied = await AppliedMigrationsAsync();

            Assert.Single(applied, migration => migration.EndsWith("_Expenses", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Migration_Expenses_IsTheLatestAppliedOne()
        {
            List<string> applied = await AppliedMigrationsAsync();

            Assert.EndsWith("_Expenses", applied[^1], StringComparison.Ordinal);
        }

        [Fact]
        public async Task Migration_Expenses_ComesAfterTheCategoriesAndExchangeRatesOne()
        {
            List<string> applied = await AppliedMigrationsAsync();

            int categories = applied.FindIndex(migration => migration.EndsWith("_CategoriesAndExchangeRates", StringComparison.Ordinal));
            int expenses = applied.FindIndex(migration => migration.EndsWith("_Expenses", StringComparison.Ordinal));
            Assert.True(categories >= 0, "The CategoriesAndExchangeRates migration is not applied.");
            Assert.True(expenses > categories, $"The Expenses migration is at {expenses} but CategoriesAndExchangeRates is at {categories}.");
        }

        [Fact]
        public async Task Migration_Expenses_LeavesNoMigrationPending()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            IEnumerable<string> pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);

            Assert.Empty(pending);
        }

        private async Task<List<string>> AppliedMigrationsAsync()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];
        }
    }
}
