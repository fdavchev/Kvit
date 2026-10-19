using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.PersonalCategories
{
    public static class PersonalCategorySteps
    {
        public static async Task<Guid> CreateAsync(HttpClient client, string name, string emoji = PersonalCategoryRequests.DefaultEmoji, string color = PersonalCategoryRequests.DefaultColor)
        {
            HttpResponseMessage response = await PersonalCategoryRequests.CreateAsync(client, name, emoji, color);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
        }

        public static async Task<Guid> CreateArchivedAsync(HttpClient client, string name)
        {
            Guid categoryId = await CreateAsync(client, name);
            HttpResponseMessage response = await PersonalCategoryRequests.ArchiveAsync(client, categoryId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            return categoryId;
        }

        public static async Task<List<JsonElement>> ListRowsAsync(HttpClient client)
        {
            return await RowsOfAsync(await PersonalCategoryRequests.ListAsync(client));
        }

        public static async Task<List<JsonElement>> ListRowsIncludingArchivedAsync(HttpClient client)
        {
            return await RowsOfAsync(await PersonalCategoryRequests.ListIncludingArchivedAsync(client));
        }

        public static async Task<List<string?>> CustomNamesAsync(HttpClient client)
        {
            List<JsonElement> rows = await ListRowsAsync(client);

            return [.. rows.Where(row => row.GetProperty("isCustom").GetBoolean()).Select(row => row.GetProperty("name").GetString())];
        }

        public static JsonElement RowOf(List<JsonElement> rows, Guid categoryId)
        {
            return Assert.Single(rows, row => row.GetProperty("id").GetGuid() == categoryId);
        }

        public static Task<string> SnapshotOfOwnerAsync(MigratedDatabase database, Guid ownerUserId)
        {
            return SnapshotAsync(database, "owner_user_id = @owner", new NpgsqlParameter("owner", ownerUserId));
        }

        public static Task<string> SnapshotOfAllAsync(MigratedDatabase database)
        {
            return SnapshotAsync(database, "TRUE");
        }

        public static async Task<string?> ArchivedAtAsync(MigratedDatabase database, Guid categoryId)
        {
            List<string?> values = await database.QueryAsync(
                "SELECT archived_at::text FROM categories WHERE id = @id",
                new NpgsqlParameter("id", categoryId));

            return Assert.Single(values);
        }

        private static async Task<string> SnapshotAsync(MigratedDatabase database, string condition, params NpgsqlParameter[] parameters)
        {
            List<string?> rows = await database.QueryAsync(
                "SELECT concat_ws('|', id, owner_user_id, key, name, emoji, color, sort_order, archived_at) FROM categories "
                + $"WHERE {condition} ORDER BY id",
                parameters);

            return string.Join("\n", rows);
        }

        private static async Task<List<JsonElement>> RowsOfAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);

            return [.. body.GetProperty("categories").EnumerateArray()];
        }
    }
}
