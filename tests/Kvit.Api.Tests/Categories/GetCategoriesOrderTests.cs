using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Categories
{
    public class GetCategoriesOrderTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        [Fact]
        public async Task List_ABuiltInCategoryInsertedLaterWithASmallerSortOrder_IsFirstInTheAnswer()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = await _app.CreateLoggedInClientAsync(form.Email, form.Password);
            await _app.ExecuteAsync(
                "INSERT INTO categories (id, owner_user_id, key, name, emoji, color, sort_order, archived_at) "
                + "VALUES (@id, NULL, 'first', NULL, @emoji, 'slate', 0, NULL)",
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("emoji", "\U0001F43E"));

            HttpResponseMessage response = await CategoryRequests.ListAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            string[] keys = [.. body.GetProperty("categories").EnumerateArray().Select(category => category.GetProperty("key").GetString() ?? string.Empty)];
            Assert.Equal(["first", .. BuiltInCategories.Keys], keys);
        }
    }
}
