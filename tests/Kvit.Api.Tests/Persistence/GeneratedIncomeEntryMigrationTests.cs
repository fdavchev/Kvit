using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class GeneratedIncomeEntryMigrationTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string UniqueViolation = "23505";

        [Fact]
        public async Task PersonalEntriesClientRequestId_IsNullable()
        {
            List<string?> nullability = await _database.QueryAsync(
                "SELECT is_nullable FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'personal_entries' AND column_name = 'client_request_id'");

            Assert.Equal("YES", Assert.Single(nullability));
        }

        [Fact]
        public async Task Insert_IncomeEntryWithoutAClientRequestId_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_database, userId);

            await InsertEntryWithoutAClientRequestIdAsync(userId, recurringIncomeId, new DateOnly(2026, 10, 1));

            Assert.Equal(1, await CountEntriesAsync(userId));
        }

        [Fact]
        public async Task Insert_SeveralEntriesWithoutAClientRequestId_AreAllAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_database, userId);

            await InsertEntryWithoutAClientRequestIdAsync(userId, recurringIncomeId, new DateOnly(2026, 9, 1));
            await InsertEntryWithoutAClientRequestIdAsync(userId, recurringIncomeId, new DateOnly(2026, 10, 1));
            await InsertEntryWithoutAClientRequestIdAsync(userId, recurringIncomeId, new DateOnly(2026, 11, 1));

            Assert.Equal(3, await CountEntriesAsync(userId));
        }

        [Fact]
        public async Task Insert_SameRecurringIncomeAndDateWithoutAClientRequestIdTwice_ViolatesTheUniqueIndexOnTheIncomeAndDate()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_database, userId);
            await InsertEntryWithoutAClientRequestIdAsync(userId, recurringIncomeId, new DateOnly(2026, 10, 1));

            await RejectedInsert.AssertAsync(() => InsertEntryWithoutAClientRequestIdAsync(userId, recurringIncomeId, new DateOnly(2026, 10, 1)), UniqueViolation);
        }

        [Fact]
        public async Task Insert_SameClientRequestIdTwice_StillViolatesTheUniqueIndexOnTheClientRequestId()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid clientRequestId = Guid.CreateVersion7();
            await InsertEntryWithAClientRequestIdAsync(userId, clientRequestId);

            await RejectedInsert.AssertAsync(() => InsertEntryWithAClientRequestIdAsync(userId, clientRequestId), UniqueViolation);
        }

        private Task InsertEntryWithoutAClientRequestIdAsync(Guid userId, Guid recurringIncomeId, DateOnly entryDate)
        {
            return _database.ExecuteAsync(
                "INSERT INTO personal_entries (id, user_id, kind, amount_minor, currency, mkd_per_eur, rate_date, entry_date, category_id, note, recurring_income_id, client_request_id, created_at, deleted_at) "
                + "VALUES (@id, @user_id, 'Income', 4500000, 'MKD', 61.5, DATE '2026-10-05', @entry_date, NULL, NULL, @recurring_income_id, NULL, now(), NULL)",
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("entry_date", NpgsqlDbType.Date) { Value = entryDate },
                new NpgsqlParameter("recurring_income_id", recurringIncomeId));
        }

        private Task InsertEntryWithAClientRequestIdAsync(Guid userId, Guid clientRequestId)
        {
            return _database.ExecuteAsync(
                "INSERT INTO personal_entries (id, user_id, kind, amount_minor, currency, mkd_per_eur, rate_date, entry_date, category_id, note, recurring_income_id, client_request_id, created_at, deleted_at) "
                + "VALUES (@id, @user_id, 'Income', 4500000, 'MKD', 61.5, DATE '2026-10-05', DATE '2026-10-04', NULL, NULL, NULL, @client_request_id, now(), NULL)",
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("client_request_id", clientRequestId));
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
