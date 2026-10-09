using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Xunit;

namespace Kvit.Api.Tests.Categories
{
    public class GetCategoriesTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        [Fact]
        public async Task List_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await CategoryRequests.ListAsync(client);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task List_SignedIn_Answers200WithOnlyTheCategoriesProperty()
        {
            JsonElement body = await ListAsync();

            string[] propertyNames = [.. body.EnumerateObject().Select(property => property.Name)];
            Assert.Equal(["categories"], propertyNames);
        }

        [Fact]
        public async Task List_SignedIn_AnswersExactlyTenCategories()
        {
            JsonElement body = await ListAsync();

            Assert.Equal(10, body.GetProperty("categories").GetArrayLength());
        }

        [Fact]
        public async Task List_EachCategory_HasExactlyTheFiveFields()
        {
            JsonElement body = await ListAsync();

            foreach (JsonElement category in body.GetProperty("categories").EnumerateArray())
            {
                string[] propertyNames = [.. category.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
                Assert.Equal(["color", "emoji", "id", "key", "sortOrder"], propertyNames);
            }
        }

        [Fact]
        public async Task List_SignedIn_AnswersTheKeysInTheStatedOrder()
        {
            JsonElement body = await ListAsync();

            Assert.Equal(BuiltInCategories.Keys, Field(body, "key", Text));
        }

        [Fact]
        public async Task List_SignedIn_AnswersTheEmojiOfEachKey()
        {
            JsonElement body = await ListAsync();

            Assert.Equal(BuiltInCategories.Emojis, Field(body, "emoji", Text));
        }

        [Fact]
        public async Task List_SignedIn_AnswersSortOrderOneToTen()
        {
            JsonElement body = await ListAsync();

            Assert.Equal(BuiltInCategories.SortOrders, Field(body, "sortOrder", element => element.GetInt32()));
        }

        [Fact]
        public async Task List_SignedIn_AnswersANonEmptyColorNameForEveryCategoryAndNoTwoAlike()
        {
            JsonElement body = await ListAsync();

            List<string> colors = Field(body, "color", Text);

            Assert.All(colors, color => Assert.False(string.IsNullOrWhiteSpace(color)));
            Assert.Equal(10, colors.Distinct(StringComparer.Ordinal).Count());
            Assert.All(colors, color => Assert.DoesNotMatch("^#", color));
        }

        [Fact]
        public async Task List_SignedIn_AnswersTenDistinctIdsThatAreNotEmpty()
        {
            JsonElement body = await ListAsync();

            List<Guid> ids = Field(body, "id", element => element.GetGuid());

            Assert.DoesNotContain(Guid.Empty, ids);
            Assert.Equal(10, ids.Distinct().Count());
        }

        [Fact]
        public async Task List_AskedTwiceByTwoUsers_AnswersTheSameIdsBothTimes()
        {
            JsonElement first = await ListAsync();
            JsonElement second = await ListAsync();

            Assert.Equal(Field(first, "id", element => element.GetGuid()), Field(second, "id", element => element.GetGuid()));
        }

        private async Task<JsonElement> ListAsync()
        {
            HttpClient client = await _app.CreateRegisteredClientAsync(RegistrationForm.Valid());

            HttpResponseMessage response = await CategoryRequests.ListAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }

        private static string Text(JsonElement element)
        {
            return element.GetString() ?? throw new InvalidOperationException("A text field of the categories answer is null.");
        }

        private static List<T> Field<T>(JsonElement body, string propertyName, Func<JsonElement, T> read)
        {
            return [.. body.GetProperty("categories").EnumerateArray().Select(category => read(category.GetProperty(propertyName)))];
        }
    }
}
