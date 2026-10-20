using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class IncomeMigrationTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string CheckViolation = "23514";
        private const string UniqueViolation = "23505";

        [Fact]
        public async Task Migration_Income_IsInTheMigrationHistoryExactlyOnceAfterPersonalCategoryNameUnique()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];

            Assert.Single(applied, migration => migration.EndsWith("_Income", StringComparison.Ordinal));
            int previous = applied.FindIndex(migration => migration.EndsWith("_PersonalCategoryNameUnique", StringComparison.Ordinal));
            int income = applied.FindIndex(migration => migration.EndsWith("_Income", StringComparison.Ordinal));
            Assert.True(previous >= 0, "The PersonalCategoryNameUnique migration is not applied.");
            Assert.True(income > previous, $"Income is at {income} but PersonalCategoryNameUnique is at {previous}.");
        }

        [Fact]
        public async Task Columns_OfRecurringIncomes_AreTheExpectedOnesWithTheirTypesAndNullability()
        {
            List<string?> columns = await _database.QueryAsync(
                "SELECT column_name || ':' || data_type || ':' || is_nullable FROM information_schema.columns "
                + "WHERE table_schema = 'public' AND table_name = 'recurring_incomes' ORDER BY column_name");

            Assert.Equal(
                [
                    "amount_minor:bigint:NO",
                    "client_request_id:uuid:NO",
                    "created_at:timestamp with time zone:NO",
                    "currency:text:NO",
                    "day_of_month:integer:YES",
                    "day_of_week:integer:YES",
                    "ends_on:date:YES",
                    "frequency:text:NO",
                    "id:uuid:NO",
                    "starts_on:date:NO",
                    "stopped_at:timestamp with time zone:YES",
                    "title:text:YES",
                    "user_id:uuid:NO",
                ],
                columns);
        }

        [Fact]
        public async Task Indexes_OfRecurringIncomes_HaveAUniqueClientRequestId()
        {
            List<string?> indexes = await _database.QueryAsync(
                "SELECT indexdef FROM pg_indexes WHERE tablename = 'recurring_incomes' AND indexdef LIKE '%UNIQUE%' AND indexdef LIKE '%(client_request_id)%'");

            Assert.Single(indexes);
        }

        [Fact]
        public async Task Insert_ValidMonthlyRecurringIncome_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await InsertRecurringAsync(userId, frequency: "Monthly", dayOfMonth: 15, dayOfWeek: null);

            Assert.Equal(1, await CountRecurringAsync(userId));
        }

        [Fact]
        public async Task Insert_ValidWeeklyRecurringIncomeWithoutTitleOrEndDate_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await InsertRecurringAsync(userId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: 3, title: null, endsOn: null);

            Assert.Equal(1, await CountRecurringAsync(userId));
        }

        [Fact]
        public async Task Insert_RecurringIncomeInEuroThatWasStopped_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await InsertRecurringAsync(userId, currency: "EUR", stoppedAt: DateTimeOffset.UtcNow);

            Assert.Equal(1, await CountRecurringAsync(userId));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(31)]
        public async Task Insert_MonthlyDayOnTheEdgesOfTheMonth_IsAccepted(int dayOfMonth)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await InsertRecurringAsync(userId, frequency: "Monthly", dayOfMonth: dayOfMonth, dayOfWeek: null);

            Assert.Equal(1, await CountRecurringAsync(userId));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public async Task Insert_WeekdayOnTheEdgesOfTheWeek_IsAccepted(int dayOfWeek)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await InsertRecurringAsync(userId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: dayOfWeek);

            Assert.Equal(1, await CountRecurringAsync(userId));
        }

        [Fact]
        public async Task Insert_EndsOnTheSameDayItStarts_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await InsertRecurringAsync(userId, startsOn: new DateOnly(2026, 10, 1), endsOn: new DateOnly(2026, 10, 1));

            Assert.Equal(1, await CountRecurringAsync(userId));
        }

        [Fact]
        public async Task Insert_FrequencyOtherThanMonthlyOrWeekly_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertRecurringAsync(userId, frequency: "Daily", dayOfMonth: 5, dayOfWeek: null), CheckViolation);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(32)]
        [InlineData(-1)]
        public async Task Insert_MonthlyDayOutsideOneTo31_IsRejected(int dayOfMonth)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertRecurringAsync(userId, frequency: "Monthly", dayOfMonth: dayOfMonth, dayOfWeek: null), CheckViolation);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(-1)]
        public async Task Insert_WeekdayOutsideZeroToSix_IsRejected(int dayOfWeek)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertRecurringAsync(userId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: dayOfWeek), CheckViolation);
        }

        [Fact]
        public async Task Insert_EndsOnBeforeStartsOn_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => InsertRecurringAsync(userId, startsOn: new DateOnly(2026, 10, 2), endsOn: new DateOnly(2026, 10, 1)),
                CheckViolation);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Insert_AmountNotAboveZero_IsRejected(long amountMinor)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertRecurringAsync(userId, amountMinor: amountMinor), CheckViolation);
        }

        [Fact]
        public async Task Insert_CurrencyOtherThanMkdOrEur_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertRecurringAsync(userId, currency: "USD"), CheckViolation);
        }

        [Fact]
        public async Task Insert_SameClientRequestIdTwice_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid clientRequestId = Guid.CreateVersion7();
            await InsertRecurringAsync(userId, clientRequestId: clientRequestId);

            await RejectedInsert.AssertAsync(() => InsertRecurringAsync(userId, clientRequestId: clientRequestId), UniqueViolation);
        }

        [Fact]
        public async Task Insert_ManyEntriesWithoutARecurringIncomeOnTheSameDay_AreAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await InsertIncomeEntryAsync(userId, null, new DateOnly(2026, 10, 19));
            await InsertIncomeEntryAsync(userId, null, new DateOnly(2026, 10, 19));
            await InsertIncomeEntryAsync(userId, null, new DateOnly(2026, 10, 19));

            Assert.Equal(3, await CountEntriesAsync(userId));
        }

        [Fact]
        public async Task Insert_EntriesOfOneRecurringIncomeOnDifferentDays_AreAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid recurringIncomeId = await InsertRecurringAsync(userId);

            await InsertIncomeEntryAsync(userId, recurringIncomeId, new DateOnly(2026, 9, 19));
            await InsertIncomeEntryAsync(userId, recurringIncomeId, new DateOnly(2026, 10, 19));

            Assert.Equal(2, await CountEntriesAsync(userId));
        }

        [Fact]
        public async Task Insert_EntriesOfTwoRecurringIncomesOnTheSameDay_AreAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid salary = await InsertRecurringAsync(userId);
            Guid rent = await InsertRecurringAsync(userId);

            await InsertIncomeEntryAsync(userId, salary, new DateOnly(2026, 10, 19));
            await InsertIncomeEntryAsync(userId, rent, new DateOnly(2026, 10, 19));

            Assert.Equal(2, await CountEntriesAsync(userId));
        }

        [Fact]
        public async Task Insert_SecondEntryOfTheSameRecurringIncomeOnTheSameDay_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid recurringIncomeId = await InsertRecurringAsync(userId);
            await InsertIncomeEntryAsync(userId, recurringIncomeId, new DateOnly(2026, 10, 19));

            await RejectedInsert.AssertAsync(() => InsertIncomeEntryAsync(userId, recurringIncomeId, new DateOnly(2026, 10, 19)), UniqueViolation);
        }

        [Fact]
        public async Task Insert_SecondEntryOfTheSameRecurringIncomeOnTheSameDayWhileTheFirstIsDeleted_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid recurringIncomeId = await InsertRecurringAsync(userId);
            await InsertIncomeEntryAsync(userId, recurringIncomeId, new DateOnly(2026, 10, 19), deletedAt: DateTimeOffset.UtcNow);

            await RejectedInsert.AssertAsync(() => InsertIncomeEntryAsync(userId, recurringIncomeId, new DateOnly(2026, 10, 19)), UniqueViolation);
        }

        private async Task<Guid> InsertRecurringAsync(
            Guid userId,
            string frequency = "Monthly",
            int? dayOfMonth = 15,
            int? dayOfWeek = null,
            long amountMinor = 4_500_000,
            string currency = "MKD",
            string? title = "Salary",
            DateOnly? startsOn = null,
            DateOnly? endsOn = null,
            DateTimeOffset? stoppedAt = null,
            Guid? clientRequestId = null)
        {
            Guid recurringIncomeId = Guid.CreateVersion7();
            await _database.ExecuteAsync(
                "INSERT INTO recurring_incomes (id, user_id, amount_minor, currency, title, frequency, day_of_month, day_of_week, starts_on, ends_on, client_request_id, created_at, stopped_at) "
                + "VALUES (@id, @user_id, @amount_minor, @currency, @title, @frequency, @day_of_month, @day_of_week, @starts_on, @ends_on, @client_request_id, now(), @stopped_at)",
                new NpgsqlParameter("id", recurringIncomeId),
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("amount_minor", amountMinor),
                new NpgsqlParameter("currency", currency),
                new NpgsqlParameter("title", NpgsqlDbType.Text) { Value = (object?)title ?? DBNull.Value },
                new NpgsqlParameter("frequency", frequency),
                new NpgsqlParameter("day_of_month", NpgsqlDbType.Integer) { Value = (object?)dayOfMonth ?? DBNull.Value },
                new NpgsqlParameter("day_of_week", NpgsqlDbType.Integer) { Value = (object?)dayOfWeek ?? DBNull.Value },
                new NpgsqlParameter("starts_on", NpgsqlDbType.Date) { Value = startsOn ?? new DateOnly(2026, 1, 1) },
                new NpgsqlParameter("ends_on", NpgsqlDbType.Date) { Value = (object?)endsOn ?? DBNull.Value },
                new NpgsqlParameter("client_request_id", clientRequestId ?? Guid.CreateVersion7()),
                new NpgsqlParameter("stopped_at", NpgsqlDbType.TimestampTz) { Value = (object?)stoppedAt ?? DBNull.Value });

            return recurringIncomeId;
        }

        private Task InsertIncomeEntryAsync(Guid userId, Guid? recurringIncomeId, DateOnly entryDate, DateTimeOffset? deletedAt = null)
        {
            return _database.ExecuteAsync(
                "INSERT INTO personal_entries (id, user_id, kind, amount_minor, currency, mkd_per_eur, rate_date, entry_date, category_id, note, recurring_income_id, client_request_id, created_at, deleted_at) "
                + "VALUES (@id, @user_id, 'Income', 4500000, 'MKD', 61.5, DATE '2026-10-19', @entry_date, NULL, NULL, @recurring_income_id, @client_request_id, now(), @deleted_at)",
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("entry_date", NpgsqlDbType.Date) { Value = entryDate },
                new NpgsqlParameter("recurring_income_id", NpgsqlDbType.Uuid) { Value = (object?)recurringIncomeId ?? DBNull.Value },
                new NpgsqlParameter("client_request_id", Guid.CreateVersion7()),
                new NpgsqlParameter("deleted_at", NpgsqlDbType.TimestampTz) { Value = (object?)deletedAt ?? DBNull.Value });
        }

        private async Task<int> CountRecurringAsync(Guid userId)
        {
            List<string?> counts = await _database.QueryAsync(
                "SELECT count(*)::text FROM recurring_incomes WHERE user_id = @user_id",
                new NpgsqlParameter("user_id", userId));

            return int.Parse(Assert.Single(counts)!);
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
