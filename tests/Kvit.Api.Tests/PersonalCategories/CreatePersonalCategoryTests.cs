using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalCategories
{
    public class CreatePersonalCategoryTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Create_ValidInput_Answers200WithTheNewRow()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalCategoryRequests.CreateAsync(user.Client, "  Pets ", "\U0001F43E", "teal");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement row = await AuthRequests.ReadJsonAsync(response);
            string[] propertyNames = [.. row.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
            Assert.Equal(PersonalCategoryRequests.RowFieldNames, propertyNames);
            Assert.NotEqual(Guid.Empty, row.GetProperty("id").GetGuid());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("key").ValueKind);
            Assert.Equal("Pets", row.GetProperty("name").GetString());
            Assert.Equal("\U0001F43E", row.GetProperty("emoji").GetString());
            Assert.Equal("teal", row.GetProperty("color").GetString());
            Assert.Equal(11, row.GetProperty("sortOrder").GetInt32());
            Assert.True(row.GetProperty("isCustom").GetBoolean());
            Assert.False(row.GetProperty("archived").GetBoolean());
        }

        [Fact]
        public async Task Create_ValidInput_StoresOneRowOwnedByTheCaller()
        {
            SignedInUser user = await _app.SignUpAsync();

            Guid categoryId = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");

            string snapshot = await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId);
            Assert.Equal($"{categoryId}|{user.UserId}|Pets|\U0001F43E|teal|11", snapshot);
        }

        [Fact]
        public async Task Create_ThreeCategories_GetSortOrders11To13InCreationOrder()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Car");

            HttpResponseMessage response = await PersonalCategoryRequests.CreateAsync(user.Client, "Kids");

            Assert.Equal(13, (await AuthRequests.ReadJsonAsync(response)).GetProperty("sortOrder").GetInt32());
        }

        [Fact]
        public async Task Create_TwoUsers_EachStartAt11()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync("Bojan");
            await PersonalCategorySteps.CreateAsync(first.Client, "Pets");

            HttpResponseMessage response = await PersonalCategoryRequests.CreateAsync(second.Client, "Pets");

            Assert.Equal(11, (await AuthRequests.ReadJsonAsync(response)).GetProperty("sortOrder").GetInt32());
        }

        [Theory]
        [InlineData("", PersonalCategoryRequests.DefaultEmoji, PersonalCategoryRequests.DefaultColor, ResultCodes.CATEGORY_NAME_INVALID)]
        [InlineData("   ", PersonalCategoryRequests.DefaultEmoji, PersonalCategoryRequests.DefaultColor, ResultCodes.CATEGORY_NAME_INVALID)]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", PersonalCategoryRequests.DefaultEmoji, PersonalCategoryRequests.DefaultColor, ResultCodes.CATEGORY_NAME_INVALID)]
        [InlineData(PersonalCategoryRequests.DefaultName, "", PersonalCategoryRequests.DefaultColor, ResultCodes.CATEGORY_EMOJI_INVALID)]
        [InlineData(PersonalCategoryRequests.DefaultName, " ", PersonalCategoryRequests.DefaultColor, ResultCodes.CATEGORY_EMOJI_INVALID)]
        [InlineData(PersonalCategoryRequests.DefaultName, "xxxxxxxxxxxxxxxxx", PersonalCategoryRequests.DefaultColor, ResultCodes.CATEGORY_EMOJI_INVALID)]
        [InlineData(PersonalCategoryRequests.DefaultName, PersonalCategoryRequests.DefaultEmoji, "", ResultCodes.CATEGORY_COLOR_INVALID)]
        [InlineData(PersonalCategoryRequests.DefaultName, PersonalCategoryRequests.DefaultEmoji, "black", ResultCodes.CATEGORY_COLOR_INVALID)]
        [InlineData(PersonalCategoryRequests.DefaultName, PersonalCategoryRequests.DefaultEmoji, "Teal", ResultCodes.CATEGORY_COLOR_INVALID)]
        [InlineData(PersonalCategoryRequests.DefaultName, PersonalCategoryRequests.DefaultEmoji, "#2a7f7a", ResultCodes.CATEGORY_COLOR_INVALID)]
        public async Task Create_InvalidField_Answers400WithItsCodeAndStoresNothing(string name, string emoji, string color, string expectedErrorCode)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalCategoryRequests.CreateAsync(user.Client, name, emoji, color);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(string.Empty, await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId));
        }

        [Theory]
        [InlineData("Pets")]
        [InlineData("pets")]
        [InlineData(" PETS ")]
        public async Task Create_NameOfAnActiveOwnCategoryInAnyLetterCase_Answers400NameTakenAndStoresNothing(string name)
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            string before = await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId);

            HttpResponseMessage response = await PersonalCategoryRequests.CreateAsync(user.Client, name);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_TAKEN);
            Assert.Equal(before, await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Create_CyrillicNameOfAnActiveOwnCategoryInAnotherLetterCase_Answers400NameTaken()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalCategorySteps.CreateAsync(user.Client, "Миленичиња");

            HttpResponseMessage response = await PersonalCategoryRequests.CreateAsync(user.Client, "миленичиња");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_TAKEN);
        }

        [Fact]
        public async Task Create_NameOfAnArchivedOwnCategory_Answers200()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Pets");

            HttpResponseMessage response = await PersonalCategoryRequests.CreateAsync(user.Client, "Pets");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Create_NameOfAnotherUsersCategory_Answers200()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync("Bojan");
            await PersonalCategorySteps.CreateAsync(first.Client, "Pets");

            HttpResponseMessage response = await PersonalCategoryRequests.CreateAsync(second.Client, "Pets");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Create_TheNameOfABuiltInKey_Answers200BecauseBuiltInsHaveNoName()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalCategoryRequests.CreateAsync(user.Client, "food");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
