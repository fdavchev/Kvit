using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public static class PersonalEntrySteps
    {
        public static async Task<Guid> AddAsync(HttpClient client, EntryInput input, Guid? clientRequestId = null)
        {
            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(client, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
        }

        public static async Task<Guid> AddAndDeleteAsync(HttpClient client, EntryInput input)
        {
            Guid entryId = await AddAsync(client, input);
            HttpResponseMessage deleted = await PersonalEntryRequests.DeleteAsync(client, entryId);

            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            return entryId;
        }

        public static async Task<Guid> AddIncomeAsync(HttpClient client, IncomeEntryInput input, Guid? clientRequestId = null)
        {
            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(client, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
        }

        public static async Task<Guid> AddIncomeAndDeleteAsync(HttpClient client, IncomeEntryInput input)
        {
            Guid entryId = await AddIncomeAsync(client, input);
            HttpResponseMessage deleted = await PersonalEntryRequests.DeleteAsync(client, entryId);

            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            return entryId;
        }

        public static async Task<JsonElement> MonthAsync(HttpClient client, string month = PersonalEntryRequests.DefaultMonth)
        {
            HttpResponseMessage response = await PersonalEntryRequests.ListAsync(client, month);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }

        public static async Task<List<Guid>> EntryIdsOfMonthAsync(HttpClient client, string month = PersonalEntryRequests.DefaultMonth)
        {
            JsonElement body = await MonthAsync(client, month);

            return [.. body.GetProperty("entries").EnumerateArray().Select(entry => entry.GetProperty("id").GetGuid())];
        }

        public static async Task<string?> ValueAsync(MigratedDatabase database, Guid entryId, string columnExpression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({columnExpression})::text FROM personal_entries WHERE id = @id",
                new NpgsqlParameter("id", entryId));

            return Assert.Single(values);
        }

        public static async Task<int> CountOfUserAsync(MigratedDatabase database, Guid userId)
        {
            List<string?> counts = await database.QueryAsync(
                "SELECT count(*)::text FROM personal_entries WHERE user_id = @user_id",
                new NpgsqlParameter("user_id", userId));

            return int.Parse(Assert.Single(counts)!);
        }

        public static async Task<string> SnapshotAsync(MigratedDatabase database)
        {
            List<string?> rows = await database.QueryAsync(
                "SELECT concat_ws('|', id, user_id, kind, amount_minor, currency, mkd_per_eur, rate_date, entry_date, category_id, note, client_request_id, created_at, updated_at, deleted_at) "
                + "FROM personal_entries ORDER BY id");

            return string.Join("\n", rows);
        }

        public static async Task<int> CountActivityAndUsageEventsOfAsync(MigratedDatabase database, Guid userId)
        {
            List<string?> counts = await database.QueryAsync(
                "SELECT ((SELECT count(*) FROM activity_events WHERE actor_user_id = @user_id) + (SELECT count(*) FROM usage_events WHERE user_id = @user_id AND type <> 'Active'))::text",
                new NpgsqlParameter("user_id", userId));

            return int.Parse(Assert.Single(counts)!);
        }

        public static Task SetEntryDateAsync(MigratedDatabase database, Guid entryId, DateOnly entryDate)
        {
            return database.ExecuteAsync(
                "UPDATE personal_entries SET entry_date = @entry_date WHERE id = @id",
                new NpgsqlParameter("entry_date", entryDate),
                new NpgsqlParameter("id", entryId));
        }

        public static Task SetDeletedAtAsync(MigratedDatabase database, Guid entryId, DateTimeOffset deletedAt)
        {
            return database.ExecuteAsync(
                "UPDATE personal_entries SET deleted_at = @deleted_at WHERE id = @id",
                new NpgsqlParameter("deleted_at", deletedAt),
                new NpgsqlParameter("id", entryId));
        }
    }
}
