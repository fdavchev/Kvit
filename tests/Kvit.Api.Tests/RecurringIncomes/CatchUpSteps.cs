using System.Globalization;
using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public static class CatchUpSteps
    {
        public const string CreatedProperty = "created";
        public const string Skopje = "Europe/Skopje";
        public const string NoonOnOctober20 = "2026-10-20T12:00:00Z";

        public static void SetNow(ExpensesApp app, string utcInstant)
        {
            app.Clock.SetTo(DateTimeOffset.Parse(utcInstant, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
        }

        public static async Task<T> AtACookieSafeTimeAsync<T>(ExpensesApp app, Func<Task<T>> action)
        {
            DateTimeOffset testClock = app.Clock.GetUtcNow();
            DateTimeOffset realNow = DateTimeOffset.UtcNow;
            app.Clock.SetTo(testClock > realNow ? testClock : realNow);
            try
            {
                return await action();
            }
            finally
            {
                app.Clock.SetTo(testClock);
            }
        }

        public static Task<SignedInUser> SignUpAsync(ExpensesApp app, string displayName = GroupsApp.OwnerName)
        {
            return AtACookieSafeTimeAsync(app, () => app.SignUpAsync(displayName));
        }

        public static async Task<SignedInUser> SignUpInAsync(ExpensesApp app, string timeZone, string displayName = GroupsApp.OwnerName)
        {
            SignedInUser user = await SignUpAsync(app, displayName);
            await app.ExecuteAsync(
                "UPDATE users SET time_zone = @time_zone WHERE id = @id",
                new NpgsqlParameter("time_zone", timeZone),
                new NpgsqlParameter("id", user.UserId));

            return user;
        }

        public static async Task<int> CatchUpAsync(HttpClient client)
        {
            HttpResponseMessage response = await RecurringIncomeRequests.CatchUpAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal([CreatedProperty], body.EnumerateObject().Select(property => property.Name));
            return body.GetProperty(CreatedProperty).GetInt32();
        }

        public static async Task<List<string>> EntryDatesAsync(MigratedDatabase database, Guid recurringIncomeId)
        {
            List<string?> dates = await database.QueryAsync(
                "SELECT entry_date::text FROM personal_entries WHERE recurring_income_id = @recurring_income_id ORDER BY entry_date",
                new NpgsqlParameter("recurring_income_id", recurringIncomeId));

            return [.. dates.Select(date => date!)];
        }

        public static async Task<string?> EntryValueAsync(MigratedDatabase database, Guid recurringIncomeId, string columnExpression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({columnExpression})::text FROM personal_entries WHERE recurring_income_id = @recurring_income_id",
                new NpgsqlParameter("recurring_income_id", recurringIncomeId));

            return Assert.Single(values);
        }

        public static async Task<List<string>> RatesSavedAsync(MigratedDatabase database, Guid recurringIncomeId)
        {
            List<string?> rows = await database.QueryAsync(
                "SELECT entry_date::text || ' ' || rate_date::text || ' ' || mkd_per_eur::text FROM personal_entries WHERE recurring_income_id = @recurring_income_id ORDER BY entry_date",
                new NpgsqlParameter("recurring_income_id", recurringIncomeId));

            return [.. rows.Select(row => row!)];
        }

        public static async Task StoreRatesAsync(ExpensesApp app, TimeSpan fetchedAgo, params StoredRate[] rates)
        {
            await app.ExecuteAsync("DELETE FROM exchange_rates");
            foreach (StoredRate rate in rates)
            {
                await app.ExecuteAsync(
                    "INSERT INTO exchange_rates (rate_date, mkd_per_eur, fetched_at) VALUES (@rate_date, @mkd_per_eur, @fetched_at)",
                    new NpgsqlParameter("rate_date", rate.RateDate),
                    new NpgsqlParameter("mkd_per_eur", rate.MkdPerEur),
                    new NpgsqlParameter("fetched_at", app.Clock.GetUtcNow() - fetchedAgo));
            }

            app.RateSource.Reset();
        }

        public static async Task<List<string>> EntryDatesOfUserAsync(MigratedDatabase database, Guid userId)
        {
            List<string?> dates = await database.QueryAsync(
                "SELECT entry_date::text FROM personal_entries WHERE user_id = @user_id ORDER BY entry_date, id",
                new NpgsqlParameter("user_id", userId));

            return [.. dates.Select(date => date!)];
        }

        public static async Task<string> EntriesSnapshotOfUserAsync(MigratedDatabase database, Guid userId)
        {
            List<string?> rows = await database.QueryAsync(
                "SELECT e::text FROM personal_entries e WHERE e.user_id = @user_id ORDER BY e.id",
                new NpgsqlParameter("user_id", userId));

            return string.Join(Environment.NewLine, rows);
        }

        public static DateOnly DateOf(string isoDate)
        {
            return DateOnly.ParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
