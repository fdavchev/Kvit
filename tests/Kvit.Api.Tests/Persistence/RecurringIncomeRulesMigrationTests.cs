using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class RecurringIncomeRulesMigrationTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string CheckViolation = "23514";

        [Fact]
        public async Task Migration_RecurringIncomeRules_IsInTheMigrationHistoryExactlyOnceAfterIncome()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];

            Assert.Single(applied, migration => migration.EndsWith("_RecurringIncomeRules", StringComparison.Ordinal));
            int previous = applied.FindIndex(migration => migration.EndsWith("_Income", StringComparison.Ordinal));
            int rules = applied.FindIndex(migration => migration.EndsWith("_RecurringIncomeRules", StringComparison.Ordinal));
            Assert.True(previous >= 0, "The Income migration is not applied.");
            Assert.True(rules > previous, $"RecurringIncomeRules is at {rules} but Income is at {previous}.");
        }

        [Fact]
        public async Task CheckConstraints_OfRecurringIncomes_AreTheOldSixPlusTheTwoNewRules()
        {
            List<string?> names = await _database.QueryAsync(
                "SELECT conname FROM pg_constraint WHERE conrelid = 'recurring_incomes'::regclass AND contype = 'c'");

            Assert.Equal(
                [
                    "ck_recurring_incomes_amount_minor_positive",
                    "ck_recurring_incomes_currency",
                    "ck_recurring_incomes_day_matches_frequency",
                    "ck_recurring_incomes_day_of_month",
                    "ck_recurring_incomes_day_of_week",
                    "ck_recurring_incomes_ends_on_not_before_starts_on",
                    "ck_recurring_incomes_frequency",
                    "ck_recurring_incomes_title_length",
                ],
                names.OfType<string>().Order(StringComparer.Ordinal));
        }

        [Fact]
        public async Task Insert_MonthlyWithADayOfMonthAndNoWeekday_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RecurringIncomeRows.InsertAsync(_database, userId, frequency: "Monthly", dayOfMonth: 15, dayOfWeek: null);

            Assert.Equal(1, await RecurringIncomeRows.CountOfUserAsync(_database, userId));
        }

        [Fact]
        public async Task Insert_WeeklyWithAWeekdayAndNoDayOfMonth_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RecurringIncomeRows.InsertAsync(_database, userId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: 6);

            Assert.Equal(1, await RecurringIncomeRows.CountOfUserAsync(_database, userId));
        }

        [Fact]
        public async Task Insert_MonthlyWithoutADayOfMonth_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => RecurringIncomeRows.InsertAsync(_database, userId, frequency: "Monthly", dayOfMonth: null, dayOfWeek: null), CheckViolation);
        }

        [Fact]
        public async Task Insert_MonthlyWithOnlyAWeekday_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => RecurringIncomeRows.InsertAsync(_database, userId, frequency: "Monthly", dayOfMonth: null, dayOfWeek: 3), CheckViolation);
        }

        [Fact]
        public async Task Insert_MonthlyWithADayOfMonthAndAWeekday_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => RecurringIncomeRows.InsertAsync(_database, userId, frequency: "Monthly", dayOfMonth: 15, dayOfWeek: 3), CheckViolation);
        }

        [Fact]
        public async Task Insert_WeeklyWithoutAWeekday_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => RecurringIncomeRows.InsertAsync(_database, userId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: null), CheckViolation);
        }

        [Fact]
        public async Task Insert_WeeklyWithOnlyADayOfMonth_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => RecurringIncomeRows.InsertAsync(_database, userId, frequency: "Weekly", dayOfMonth: 15, dayOfWeek: null), CheckViolation);
        }

        [Fact]
        public async Task Insert_WeeklyWithAWeekdayAndADayOfMonth_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => RecurringIncomeRows.InsertAsync(_database, userId, frequency: "Weekly", dayOfMonth: 15, dayOfWeek: 3), CheckViolation);
        }

        [Fact]
        public async Task Insert_WithoutATitle_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RecurringIncomeRows.InsertAsync(_database, userId, title: null);

            Assert.Equal(1, await RecurringIncomeRows.CountOfUserAsync(_database, userId));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(60)]
        public async Task Insert_TitleOfOneToSixtyCharacters_IsAccepted(int length)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RecurringIncomeRows.InsertAsync(_database, userId, title: new string('a', length));

            Assert.Equal(1, await RecurringIncomeRows.CountOfUserAsync(_database, userId));
        }

        [Fact]
        public async Task Insert_TitleOf60CyrillicCharacters_IsAcceptedBecauseTheLengthCountsCharactersNotBytes()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RecurringIncomeRows.InsertAsync(_database, userId, title: new string('Ж', 60));

            Assert.Equal(1, await RecurringIncomeRows.CountOfUserAsync(_database, userId));
        }

        [Fact]
        public async Task Insert_TitleOf61Characters_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => RecurringIncomeRows.InsertAsync(_database, userId, title: new string('a', 61)), CheckViolation);
        }

        [Fact]
        public async Task Insert_EmptyTitle_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => RecurringIncomeRows.InsertAsync(_database, userId, title: string.Empty), CheckViolation);
        }
    }
}
