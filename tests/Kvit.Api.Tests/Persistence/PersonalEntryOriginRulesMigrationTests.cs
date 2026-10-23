using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class PersonalEntryOriginRulesMigrationTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string CheckViolation = "23514";

        private static readonly Guid BuiltInFoodId = Guid.Parse("c47e9a10-5f3b-4d2e-9a61-000000000001");

        [Fact]
        public async Task Migration_PersonalEntryOriginRules_IsInTheMigrationHistoryExactlyOnceAfterGeneratedIncomeEntries()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];

            Assert.Single(applied, migration => migration.EndsWith("_PersonalEntryOriginRules", StringComparison.Ordinal));
            int previous = applied.FindIndex(migration => migration.EndsWith("_GeneratedIncomeEntries", StringComparison.Ordinal));
            int rules = applied.FindIndex(migration => migration.EndsWith("_PersonalEntryOriginRules", StringComparison.Ordinal));
            Assert.True(previous >= 0, "The GeneratedIncomeEntries migration is not applied.");
            Assert.True(rules > previous, $"PersonalEntryOriginRules is at {rules} but GeneratedIncomeEntries is at {previous}.");
        }

        [Fact]
        public async Task CheckConstraints_IncludeTheThreeOriginRules()
        {
            List<string?> names = await _database.QueryAsync(
                "SELECT conname FROM pg_constraint WHERE conrelid = 'personal_entries'::regclass AND contype = 'c'");

            Assert.Contains("ck_personal_entries_income_has_no_category", names);
            Assert.Contains("ck_personal_entries_generated_is_income", names);
            Assert.Contains("ck_personal_entries_has_origin", names);
        }

        [Fact]
        public async Task Insert_IncomeWithACategory_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertAsync(userId, "Income", BuiltInFoodId, null, Guid.CreateVersion7()), CheckViolation);
        }

        [Fact]
        public async Task Insert_SpendingFromARecurringIncome_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_database, userId);

            await RejectedInsert.AssertAsync(() => InsertAsync(userId, "Spending", BuiltInFoodId, recurringIncomeId, Guid.CreateVersion7()), CheckViolation);
        }

        [Theory]
        [InlineData("Income")]
        [InlineData("Spending")]
        public async Task Insert_WithoutAClientRequestIdAndWithoutARecurringIncome_IsRejected(string kind)
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid? categoryId = kind == "Spending" ? BuiltInFoodId : null;

            await RejectedInsert.AssertAsync(() => InsertAsync(userId, kind, categoryId, null, null), CheckViolation);
        }

        [Fact]
        public async Task Insert_SpendingTheUserAdded_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await InsertAsync(userId, "Spending", BuiltInFoodId, null, Guid.CreateVersion7());

            Assert.Equal(1, await CountEntriesAsync(userId));
        }

        [Fact]
        public async Task Insert_IncomeTheUserAddedWithoutACategory_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await InsertAsync(userId, "Income", null, null, Guid.CreateVersion7());

            Assert.Equal(1, await CountEntriesAsync(userId));
        }

        [Fact]
        public async Task Insert_IncomeFromARecurringIncomeWithoutAClientRequestId_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_database, userId);

            await InsertAsync(userId, "Income", null, recurringIncomeId, null);

            Assert.Equal(1, await CountEntriesAsync(userId));
        }

        private Task InsertAsync(Guid userId, string kind, Guid? categoryId, Guid? recurringIncomeId, Guid? clientRequestId)
        {
            return _database.ExecuteAsync(
                "INSERT INTO personal_entries (id, user_id, kind, amount_minor, currency, mkd_per_eur, rate_date, entry_date, category_id, note, recurring_income_id, client_request_id, created_at) "
                + "VALUES (@id, @user_id, @kind, 100000, 'MKD', 61.5, DATE '2026-10-19', DATE '2026-10-19', @category_id, NULL, @recurring_income_id, @client_request_id, now())",
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("kind", kind),
                new NpgsqlParameter("category_id", NpgsqlDbType.Uuid) { Value = (object?)categoryId ?? DBNull.Value },
                new NpgsqlParameter("recurring_income_id", NpgsqlDbType.Uuid) { Value = (object?)recurringIncomeId ?? DBNull.Value },
                new NpgsqlParameter("client_request_id", NpgsqlDbType.Uuid) { Value = (object?)clientRequestId ?? DBNull.Value });
        }

        private async Task<int> CountEntriesAsync(Guid userId)
        {
            List<string?> counts = await _database.QueryAsync(
                "SELECT count(*)::text FROM personal_entries WHERE user_id = @user_id",
                new NpgsqlParameter("user_id", userId));

            return int.Parse(Assert.Single(counts)!);
        }
    }
}
