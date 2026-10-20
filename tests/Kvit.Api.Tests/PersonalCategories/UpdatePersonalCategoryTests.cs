using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalCategories
{
    public class UpdatePersonalCategoryTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Update_OwnCategoryWithValidInput_Answers204AndStoresTheTrimmedValues()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");

            HttpResponseMessage response = await PersonalCategoryRequests.UpdateAsync(user.Client, pets, " Dogs ", "\U0001F415", "orange");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal($"{pets}|{user.UserId}|Dogs|\U0001F415|orange|11", await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Update_SameNameInAnotherLetterCase_Answers204()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");

            HttpResponseMessage response = await PersonalCategoryRequests.UpdateAsync(user.Client, pets, "PETS");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(["PETS"], await PersonalCategorySteps.CustomNamesAsync(user.Client));
        }

        [Theory]
        [InlineData("", PersonalCategoryRequests.DefaultEmoji, PersonalCategoryRequests.DefaultColor, ResultCodes.CATEGORY_NAME_INVALID)]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", PersonalCategoryRequests.DefaultEmoji, PersonalCategoryRequests.DefaultColor, ResultCodes.CATEGORY_NAME_INVALID)]
        [InlineData(PersonalCategoryRequests.DefaultName, "", PersonalCategoryRequests.DefaultColor, ResultCodes.CATEGORY_EMOJI_INVALID)]
        [InlineData(PersonalCategoryRequests.DefaultName, "xxxxxxxxxxxxxxxxx", PersonalCategoryRequests.DefaultColor, ResultCodes.CATEGORY_EMOJI_INVALID)]
        [InlineData(PersonalCategoryRequests.DefaultName, "abc", PersonalCategoryRequests.DefaultColor, ResultCodes.CATEGORY_EMOJI_INVALID)]
        [InlineData(PersonalCategoryRequests.DefaultName, " x ", PersonalCategoryRequests.DefaultColor, ResultCodes.CATEGORY_EMOJI_INVALID)]
        [InlineData(PersonalCategoryRequests.DefaultName, PersonalCategoryRequests.DefaultEmoji, "black", ResultCodes.CATEGORY_COLOR_INVALID)]
        public async Task Update_InvalidField_Answers400WithItsCodeAndChangesNothing(string name, string emoji, string color, string expectedErrorCode)
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            string before = await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId);

            HttpResponseMessage response = await PersonalCategoryRequests.UpdateAsync(user.Client, pets, name, emoji, color);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(before, await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Update_NameOfAnotherActiveOwnCategory_Answers400NameTakenAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            await PersonalCategorySteps.CreateAsync(user.Client, "Kids");
            string before = await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId);

            HttpResponseMessage response = await PersonalCategoryRequests.UpdateAsync(user.Client, pets, "kids", "\U0001F9F8", "pink");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_TAKEN);
            Assert.Equal(before, await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Update_NameOfAnArchivedOwnCategory_Answers204()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Kids");

            HttpResponseMessage response = await PersonalCategoryRequests.UpdateAsync(user.Client, pets, "Kids");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Update_AnArchivedCategoryKeepingTheNameOfAnActiveOne_Answers204AndKeepsItArchived()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid oldPets = await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Pets");
            await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            string? archivedAt = await PersonalCategorySteps.ArchivedAtAsync(_app, oldPets);

            HttpResponseMessage response = await PersonalCategoryRequests.UpdateAsync(user.Client, oldPets, "Pets", "\U0001F415", "orange");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.NotNull(archivedAt);
            Assert.Equal(archivedAt, await PersonalCategorySteps.ArchivedAtAsync(_app, oldPets));
        }

        [Fact]
        public async Task Update_ABuiltInCategory_Answers404CategoryNotFoundAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid food = Guid.Parse("c47e9a10-5f3b-4d2e-9a61-000000000001");
            string before = await PersonalCategorySteps.SnapshotOfAllAsync(_app);

            HttpResponseMessage response = await PersonalCategoryRequests.UpdateAsync(user.Client, food, "Food");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
            Assert.Equal(before, await PersonalCategorySteps.SnapshotOfAllAsync(_app));
        }

        [Fact]
        public async Task Update_UnknownCategory_Answers404CategoryNotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalCategoryRequests.UpdateAsync(user.Client, Guid.CreateVersion7(), "Pets");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
        }
    }
}
