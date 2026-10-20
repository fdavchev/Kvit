using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Categories;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.PersonalCategories
{
    public class ListPersonalCategoriesTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task List_NewUser_Answers200WithOnlyTheCategoriesProperty()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalCategoryRequests.ListAsync(user.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            string[] propertyNames = [.. body.EnumerateObject().Select(property => property.Name)];
            Assert.Equal(["categories"], propertyNames);
        }

        [Fact]
        public async Task List_NewUser_AnswersTheTenBuiltInsInOrderWithTheirKeysAndNoName()
        {
            SignedInUser user = await _app.SignUpAsync();

            List<JsonElement> rows = await PersonalCategorySteps.ListRowsAsync(user.Client);

            Assert.Equal(BuiltInCategories.Keys, rows.Select(row => row.GetProperty("key").GetString()));
            Assert.Equal(BuiltInCategories.Emojis, rows.Select(row => row.GetProperty("emoji").GetString()));
            Assert.Equal(BuiltInCategories.SortOrders, rows.Select(row => row.GetProperty("sortOrder").GetInt32()));
            Assert.All(rows, row => Assert.Equal(JsonValueKind.Null, row.GetProperty("name").ValueKind));
            Assert.All(rows, row => Assert.False(row.GetProperty("isCustom").GetBoolean()));
            Assert.All(rows, row => Assert.False(row.GetProperty("archived").GetBoolean()));
        }

        [Fact]
        public async Task List_EachRow_HasExactlyTheEightFields()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalCategorySteps.CreateAsync(user.Client, "Pets");

            List<JsonElement> rows = await PersonalCategorySteps.ListRowsAsync(user.Client);

            Assert.All(rows, row =>
            {
                string[] propertyNames = [.. row.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
                Assert.Equal(PersonalCategoryRequests.RowFieldNames, propertyNames);
            });
        }

        [Fact]
        public async Task List_UserWithOwnCategories_AnswersThemAfterTheBuiltInsInCreationOrder()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets", "\U0001F43E", "teal");
            Guid kids = await PersonalCategorySteps.CreateAsync(user.Client, "Kids", "\U0001F9F8", "pink");

            List<JsonElement> rows = await PersonalCategorySteps.ListRowsAsync(user.Client);

            Assert.Equal(12, rows.Count);
            Assert.Equal(BuiltInCategories.Keys, rows.Take(10).Select(row => row.GetProperty("key").GetString()));
            Assert.Equal([pets, kids], rows.Skip(10).Select(row => row.GetProperty("id").GetGuid()));
            JsonElement kidsRow = rows[11];
            Assert.Equal(JsonValueKind.Null, kidsRow.GetProperty("key").ValueKind);
            Assert.Equal("Kids", kidsRow.GetProperty("name").GetString());
            Assert.Equal("\U0001F9F8", kidsRow.GetProperty("emoji").GetString());
            Assert.Equal("pink", kidsRow.GetProperty("color").GetString());
            Assert.Equal(12, kidsRow.GetProperty("sortOrder").GetInt32());
            Assert.True(kidsRow.GetProperty("isCustom").GetBoolean());
            Assert.False(kidsRow.GetProperty("archived").GetBoolean());
        }

        [Fact]
        public async Task List_WithoutTheArchivedFilter_LeavesArchivedOwnCategoriesOut()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Car");

            List<string?> names = await PersonalCategorySteps.CustomNamesAsync(user.Client);

            Assert.Equal(["Pets"], names);
        }

        [Fact]
        public async Task List_IncludingArchived_AnswersArchivedOwnCategoriesMarkedArchived()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            Guid car = await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Car");

            List<JsonElement> rows = await PersonalCategorySteps.ListRowsIncludingArchivedAsync(user.Client);

            Assert.Equal(12, rows.Count);
            Assert.Equal([pets, car], rows.Skip(10).Select(row => row.GetProperty("id").GetGuid()));
            Assert.False(PersonalCategorySteps.RowOf(rows, pets).GetProperty("archived").GetBoolean());
            Assert.True(PersonalCategorySteps.RowOf(rows, car).GetProperty("archived").GetBoolean());
        }

        [Fact]
        public async Task List_IncludeArchivedFalse_LeavesArchivedOwnCategoriesOut()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Car");

            HttpResponseMessage response = await user.Client.GetAsync($"{PersonalCategoryRequests.CategoriesPath}?includeArchived=false", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(10, body.GetProperty("categories").GetArrayLength());
        }

        [Fact]
        public async Task List_AnotherUsersCategories_AreNeverInTheAnswerEvenIncludingArchived()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            await PersonalCategorySteps.CreateAsync(owner.Client, "Pets");
            await PersonalCategorySteps.CreateArchivedAsync(owner.Client, "Car");

            List<JsonElement> rows = await PersonalCategorySteps.ListRowsIncludingArchivedAsync(other.Client);

            Assert.Equal(BuiltInCategories.Keys, rows.Select(row => row.GetProperty("key").GetString()));
        }

        [Fact]
        public async Task List_ARenamedCategory_ShowsTheNewValues()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            await PersonalCategoryRequests.UpdateAsync(user.Client, pets, "Dogs", "\U0001F415", "orange");

            JsonElement row = PersonalCategorySteps.RowOf(await PersonalCategorySteps.ListRowsAsync(user.Client), pets);

            Assert.Equal("Dogs", row.GetProperty("name").GetString());
            Assert.Equal("\U0001F415", row.GetProperty("emoji").GetString());
            Assert.Equal("orange", row.GetProperty("color").GetString());
        }
    }
}
