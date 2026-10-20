using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class PersonalEntriesMigrationTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string ForeignKeyViolation = "23503";
        private const string CheckViolation = "23514";
        private const string UniqueViolation = "23505";
        private const string NotNullViolation = "23502";
        private const string RestrictViolation = "23001";

        private static readonly Guid BuiltInFoodId = Guid.Parse("c47e9a10-5f3b-4d2e-9a61-000000000001");

        [Fact]
        public async Task Migration_PersonalEntries_IsInTheMigrationHistoryExactlyOnceAfterPersonalCategories()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];

            Assert.Single(applied, migration => migration.EndsWith("_PersonalEntries", StringComparison.Ordinal));
            int previous = applied.FindIndex(migration => migration.EndsWith("_PersonalCategories", StringComparison.Ordinal));
            int personalEntries = applied.FindIndex(migration => migration.EndsWith("_PersonalEntries", StringComparison.Ordinal));
            Assert.True(previous >= 0, "The PersonalCategories migration is not applied.");
            Assert.True(personalEntries > previous, $"PersonalEntries is at {personalEntries} but PersonalCategories is at {previous}.");
        }

        [Fact]
        public async Task Columns_AreTheExpectedOnesWithTheirTypesAndNullability()
        {
            List<string?> columns = await _database.QueryAsync(
                "SELECT column_name || ':' || data_type || ':' || is_nullable FROM information_schema.columns "
                + "WHERE table_schema = 'public' AND table_name = 'personal_entries' ORDER BY column_name");

            Assert.Equal(
                [
                    "amount_minor:bigint:NO",
                    "category_id:uuid:YES",
                    "client_request_id:uuid:NO",
                    "created_at:timestamp with time zone:NO",
                    "currency:text:NO",
                    "deleted_at:timestamp with time zone:YES",
                    "entry_date:date:NO",
                    "id:uuid:NO",
                    "kind:text:NO",
                    "mkd_per_eur:numeric:NO",
                    "note:text:YES",
                    "rate_date:date:NO",
                    "recurring_income_id:uuid:YES",
                    "updated_at:timestamp with time zone:YES",
                    "user_id:uuid:NO",
                ],
                columns);
        }

        [Fact]
        public async Task CheckConstraints_AreExactlyTheNamedOnes()
        {
            List<string?> names = await _database.QueryAsync(
                "SELECT conname FROM pg_constraint WHERE conrelid = 'personal_entries'::regclass AND contype = 'c' ORDER BY conname");

            Assert.Equal(
                [
                    "ck_personal_entries_amount_minor_positive",
                    "ck_personal_entries_currency",
                    "ck_personal_entries_kind",
                    "ck_personal_entries_mkd_per_eur_positive",
                    "ck_personal_entries_note_length",
                    "ck_personal_entries_spending_has_category",
                ],
                names);
        }

        [Fact]
        public async Task ForeignKeys_AreToUsersCategoriesAndRecurringIncomesAndEveryOneRestrictsDeletes()
        {
            List<string?> foreignKeys = await _database.QueryAsync(
                "SELECT conname || ':' || confrelid::regclass::text || ':' || confdeltype::text FROM pg_constraint "
                + "WHERE conrelid = 'personal_entries'::regclass AND contype = 'f' ORDER BY conname");

            Assert.Equal(
                [
                    "fk_personal_entries_categories_category_id:categories:r",
                    "fk_personal_entries_recurring_incomes_recurring_income_id:recurring_incomes:r",
                    "fk_personal_entries_users_user_id:users:r"
                ],
                foreignKeys);
        }

        [Fact]
        public async Task Indexes_AreTheUniqueClientRequestIdTheUniqueRecurringIncomeAndDateTheUserAndDateAndTheCategory()
        {
            List<string?> indexes = await _database.QueryAsync(
                "SELECT indexname || ':' || indexdef FROM pg_indexes WHERE tablename = 'personal_entries' AND indexname <> 'pk_personal_entries' ORDER BY indexname");

            Assert.Equal(4, indexes.Count);
            Assert.Contains("ix_personal_entries_category_id:", indexes[0]);
            Assert.Contains("ix_personal_entries_client_request_id:CREATE UNIQUE INDEX", indexes[1]);
            Assert.Contains("(client_request_id)", indexes[1]);
            Assert.Contains("ix_personal_entries_recurring_income_id_entry_date:CREATE UNIQUE INDEX", indexes[2]);
            Assert.Contains("(recurring_income_id, entry_date)", indexes[2]);
            Assert.Contains("ix_personal_entries_user_id_entry_date:", indexes[3]);
            Assert.Contains("(user_id, entry_date)", indexes[3]);
        }

        [Fact]
        public async Task Insert_ValidSpendingRow_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await InsertAsync(userId);

            List<string?> counts = await _database.QueryAsync("SELECT count(*)::text FROM personal_entries WHERE user_id = @user_id", new NpgsqlParameter("user_id", userId));
            Assert.Equal("1", Assert.Single(counts));
        }

        [Fact]
        public async Task Insert_KindOtherThanSpendingOrIncome_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertAsync(userId, kind: "Savings"), CheckViolation);
        }

        [Fact]
        public async Task Insert_ValidIncomeRowWithoutACategory_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await InsertRowAsync(userId, "Income", 4_500_000, "MKD", null, null, null);

            List<string?> kinds = await _database.QueryAsync("SELECT kind FROM personal_entries WHERE user_id = @user_id", new NpgsqlParameter("user_id", userId));
            Assert.Equal("Income", Assert.Single(kinds));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Insert_AmountNotAboveZero_IsRejected(long amountMinor)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertAsync(userId, amountMinor: amountMinor), CheckViolation);
        }

        [Fact]
        public async Task Insert_CurrencyOtherThanMkdOrEur_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertAsync(userId, currency: "USD"), CheckViolation);
        }

        [Fact]
        public async Task Insert_SpendingWithoutACategory_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertWithoutCategoryAsync(userId), CheckViolation);
        }

        [Fact]
        public async Task Insert_EmptyNote_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertAsync(userId, note: ""), CheckViolation);
        }

        [Fact]
        public async Task Insert_UserThatDoesNotExist_IsRejected()
        {
            await RejectedInsert.AssertAsync(() => InsertAsync(Guid.CreateVersion7()), ForeignKeyViolation);
        }

        [Fact]
        public async Task Insert_CategoryThatDoesNotExist_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertAsync(userId, categoryId: Guid.CreateVersion7()), ForeignKeyViolation);
        }

        [Fact]
        public async Task Insert_SameClientRequestIdTwice_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid clientRequestId = Guid.CreateVersion7();
            await InsertAsync(userId, clientRequestId: clientRequestId);

            await RejectedInsert.AssertAsync(() => InsertAsync(userId, clientRequestId: clientRequestId), UniqueViolation);
        }

        [Fact]
        public async Task Insert_WithoutAKind_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertAsync(userId, kind: null), NotNullViolation);
        }

        [Fact]
        public async Task Delete_AUserWithAnEntry_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            await InsertAsync(userId);

            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync("DELETE FROM users WHERE id = @id", new NpgsqlParameter("id", userId)),
                RestrictViolation);
        }

        [Fact]
        public async Task Delete_ACategoryOnAnEntry_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid categoryId = Guid.CreateVersion7();
            await _database.ExecuteAsync(
                "INSERT INTO categories (id, owner_user_id, key, name, emoji, color, sort_order) VALUES (@id, @owner, NULL, 'Pets', 'x', 'teal', 11)",
                new NpgsqlParameter("id", categoryId),
                new NpgsqlParameter("owner", userId));
            await InsertAsync(userId, categoryId: categoryId);

            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync("DELETE FROM categories WHERE id = @id", new NpgsqlParameter("id", categoryId)),
                RestrictViolation);
        }

        private Task InsertAsync(
            Guid userId,
            string? kind = "Spending",
            long amountMinor = 100_000,
            string currency = "MKD",
            Guid? categoryId = null,
            string? note = null,
            Guid? clientRequestId = null)
        {
            return InsertRowAsync(userId, kind, amountMinor, currency, categoryId ?? BuiltInFoodId, note, clientRequestId);
        }

        private Task InsertWithoutCategoryAsync(Guid userId)
        {
            return InsertRowAsync(userId, "Spending", 100_000, "MKD", null, null, null);
        }

        private Task InsertRowAsync(Guid userId, string? kind, long amountMinor, string currency, Guid? categoryId, string? note, Guid? clientRequestId)
        {
            return _database.ExecuteAsync(
                "INSERT INTO personal_entries (id, user_id, kind, amount_minor, currency, mkd_per_eur, rate_date, entry_date, category_id, note, client_request_id, created_at) "
                + "VALUES (@id, @user_id, @kind, @amount_minor, @currency, 61.5, DATE '2026-10-19', DATE '2026-10-19', @category_id, @note, @client_request_id, now())",
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("kind", NpgsqlDbType.Text) { Value = (object?)kind ?? DBNull.Value },
                new NpgsqlParameter("amount_minor", amountMinor),
                new NpgsqlParameter("currency", currency),
                new NpgsqlParameter("category_id", NpgsqlDbType.Uuid) { Value = (object?)categoryId ?? DBNull.Value },
                new NpgsqlParameter("note", NpgsqlDbType.Text) { Value = (object?)note ?? DBNull.Value },
                new NpgsqlParameter("client_request_id", clientRequestId ?? Guid.CreateVersion7()));
        }
    }
}
