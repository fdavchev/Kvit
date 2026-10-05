using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Categories
{
    public class GetCategoriesFilteringTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        [Fact]
        public async Task List_ACustomCategoryOfTheCaller_IsNotInTheAnswer()
        {
            RegistrationForm form = RegistrationForm.Valid();
            Guid userId = await _app.CreateAccountAsync(form);
            HttpClient client = await _app.CreateLoggedInClientAsync(form.Email, form.Password);
            await InsertCategoryAsync(userId, key: null, name: "Pets", sortOrder: 11, isArchived: false);

            string[] keys = await ListKeysAsync(client);

            Assert.Equal(BuiltInCategories.Keys, keys);
        }

        [Fact]
        public async Task List_AnArchivedBuiltInCategory_IsNotInTheAnswer()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = await _app.CreateLoggedInClientAsync(form.Email, form.Password);
            await InsertCategoryAsync(null, key: "retired", name: null, sortOrder: 12, isArchived: true);

            string[] keys = await ListKeysAsync(client);

            Assert.Equal(BuiltInCategories.Keys, keys);
        }

        private static async Task<string[]> ListKeysAsync(HttpClient client)
        {
            HttpResponseMessage response = await CategoryRequests.ListAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            return [.. body.GetProperty("categories").EnumerateArray().Select(category => category.GetProperty("key").GetString() ?? string.Empty)];
        }

        private Task InsertCategoryAsync(Guid? ownerUserId, string? key, string? name, int sortOrder, bool isArchived)
        {
            return _app.ExecuteAsync(
                "INSERT INTO categories (id, owner_user_id, key, name, emoji, color, sort_order, archived_at) "
                + "VALUES (@id, @owner, @key, @name, @emoji, @color, @sort_order, @archived_at)",
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("owner", (object?)ownerUserId ?? DBNull.Value),
                new NpgsqlParameter("key", (object?)key ?? DBNull.Value),
                new NpgsqlParameter("name", (object?)name ?? DBNull.Value),
                new NpgsqlParameter("emoji", "\U0001F43E"),
                new NpgsqlParameter("color", "slate"),
                new NpgsqlParameter("sort_order", sortOrder),
                new NpgsqlParameter("archived_at", isArchived ? (object)DateTimeOffset.UtcNow : DBNull.Value));
        }
    }
}
