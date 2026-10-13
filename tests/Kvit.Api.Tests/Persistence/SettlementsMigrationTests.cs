using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class SettlementsMigrationTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Fact]
        public async Task Migration_Settlements_IsInTheMigrationHistoryExactlyOnce()
        {
            List<string> applied = await AppliedMigrationsAsync();

            Assert.Single(applied, migration => migration.EndsWith("_Settlements", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Migration_Settlements_ComesAfterTheExpensesOne()
        {
            List<string> applied = await AppliedMigrationsAsync();

            int expenses = applied.FindIndex(migration => migration.EndsWith("_Expenses", StringComparison.Ordinal));
            int settlements = applied.FindIndex(migration => migration.EndsWith("_Settlements", StringComparison.Ordinal));
            Assert.True(expenses >= 0, "The Expenses migration is not applied.");
            Assert.True(settlements > expenses, $"The Settlements migration is at {settlements} but Expenses is at {expenses}.");
        }

        [Fact]
        public async Task Migration_Settlements_LeavesNoMigrationPending()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            IEnumerable<string> pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);

            Assert.Empty(pending);
        }

        [Fact]
        public async Task Migration_Settlements_LetsTheActivityEventTypeCheckAllowSettlementRestored()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid actorUserId = await UserRows.AddAsync(_database);

            await GroupRows.InsertActivityEventAsync(_database, groupId, actorUserId, "SettlementRestored");

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_database, groupId, "SettlementRestored"));
        }

        [Fact]
        public async Task Migration_Settlements_KeepsTheActivityEventTypeCheckRejectingUnknownNames()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid actorUserId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertActivityEventAsync(_database, groupId, actorUserId, "SettlementRestoredTwice"),
                PostgresErrorCodes.CheckViolation);
        }

        private async Task<List<string>> AppliedMigrationsAsync()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];
        }
    }
}
