using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class CatchUpInsertOrderTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task CatchUp_SeveralIncomes_InsertsTheEntriesIncomeByIncomeInCreationOrderAndEachIncomeOldestDateFirst()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid createdSecond = await RecurringIncomeRows.InsertAsync(_app, user.UserId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: 3, startsOn: new DateOnly(2026, 6, 3));
            Guid createdFirst = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 3, 1));
            Guid createdThirdTiedOne = await RecurringIncomeRows.InsertAsync(_app, user.UserId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: 1, startsOn: new DateOnly(2026, 8, 3));
            Guid createdThirdTiedTwo = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 2, startsOn: new DateOnly(2026, 5, 1));
            await SetCreatedAtAsync(createdFirst, "2026-06-01T08:00:00Z");
            await SetCreatedAtAsync(createdSecond, "2026-06-02T08:00:00Z");
            await SetCreatedAtAsync(createdThirdTiedOne, "2026-06-03T08:00:00Z");
            await SetCreatedAtAsync(createdThirdTiedTwo, "2026-06-03T08:00:00Z");
            await StartLoggingInsertOrderAsync();
            try
            {
                int created = await CatchUpSteps.CatchUpAsync(user.Client);

                List<string> expectedOrder = await EntriesByIncomeCreationThenDateAsync(user.UserId);
                Assert.Equal(created, expectedOrder.Count);
                Assert.True(created > 40, $"Expected many entries to make a random order visible, got {created}.");
                Assert.Equal(expectedOrder, await InsertOrderAsync());
            }
            finally
            {
                await StopLoggingInsertOrderAsync();
            }
        }

        private Task SetCreatedAtAsync(Guid recurringIncomeId, string utcInstant)
        {
            return _app.ExecuteAsync(
                "UPDATE recurring_incomes SET created_at = @created_at::timestamptz WHERE id = @id",
                new NpgsqlParameter("created_at", utcInstant),
                new NpgsqlParameter("id", recurringIncomeId));
        }

        private Task StartLoggingInsertOrderAsync()
        {
            return _app.ExecuteAsync(
                "CREATE TABLE entry_insert_order (position bigserial PRIMARY KEY, recurring_income_id uuid NOT NULL, entry_date date NOT NULL); "
                + "CREATE FUNCTION log_entry_insert_order() RETURNS trigger LANGUAGE plpgsql AS $$ "
                + "BEGIN INSERT INTO entry_insert_order (recurring_income_id, entry_date) VALUES (NEW.recurring_income_id, NEW.entry_date); RETURN NEW; END $$; "
                + "CREATE TRIGGER log_entry_insert_order BEFORE INSERT ON personal_entries FOR EACH ROW EXECUTE FUNCTION log_entry_insert_order();");
        }

        private Task StopLoggingInsertOrderAsync()
        {
            return _app.ExecuteAsync(
                "DROP TRIGGER IF EXISTS log_entry_insert_order ON personal_entries; DROP FUNCTION IF EXISTS log_entry_insert_order(); DROP TABLE IF EXISTS entry_insert_order;");
        }

        private async Task<List<string>> InsertOrderAsync()
        {
            List<string?> rows = await _app.QueryAsync("SELECT recurring_income_id::text || ' ' || entry_date::text FROM entry_insert_order ORDER BY position");

            return [.. rows.Select(row => row!)];
        }

        private async Task<List<string>> EntriesByIncomeCreationThenDateAsync(Guid userId)
        {
            List<string?> rows = await _app.QueryAsync(
                "SELECT income.id::text || ' ' || entry.entry_date::text FROM personal_entries entry "
                + "JOIN recurring_incomes income ON income.id = entry.recurring_income_id "
                + "WHERE entry.user_id = @user_id ORDER BY income.created_at, income.id, entry.entry_date",
                new NpgsqlParameter("user_id", userId));

            return [.. rows.Select(row => row!)];
        }
    }
}
