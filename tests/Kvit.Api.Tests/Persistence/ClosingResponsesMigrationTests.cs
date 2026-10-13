using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class ClosingResponsesMigrationTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Fact]
        public async Task Migration_ClosingResponses_IsInTheMigrationHistoryExactlyOnce()
        {
            List<string> applied = await AppliedMigrationsAsync();

            Assert.Single(applied, migration => migration.EndsWith("_ClosingResponses", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Migration_ClosingResponses_ComesAfterTheSettlementsOne()
        {
            List<string> applied = await AppliedMigrationsAsync();

            int settlements = applied.FindIndex(migration => migration.EndsWith("_Settlements", StringComparison.Ordinal));
            int closingResponses = applied.FindIndex(migration => migration.EndsWith("_ClosingResponses", StringComparison.Ordinal));
            Assert.True(settlements >= 0, "The Settlements migration is not applied.");
            Assert.True(closingResponses > settlements, $"The ClosingResponses migration is at {closingResponses} but Settlements is at {settlements}.");
        }

        [Fact]
        public async Task Migration_ClosingResponses_LeavesNoMigrationPending()
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
