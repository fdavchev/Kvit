using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.PersonalCategories
{
    public class ArchivePersonalCategoryTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        private static readonly Guid BuiltInFoodId = Guid.Parse("c47e9a10-5f3b-4d2e-9a61-000000000001");

        [Fact]
        public async Task Archive_OwnActiveCategory_Answers204AndStoresTheArchiveMoment()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");

            HttpResponseMessage response = await PersonalCategoryRequests.ArchiveAsync(user.Client, pets);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.NotNull(await PersonalCategorySteps.ArchivedAtAsync(_app, pets));
            Assert.Empty(await PersonalCategorySteps.CustomNamesAsync(user.Client));
        }

        [Fact]
        public async Task Archive_OwnArchivedCategory_Answers204AndKeepsTheFirstArchiveMoment()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Pets");
            await SetArchivedAtAsync(pets, new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero));
            string? firstArchivedAt = await PersonalCategorySteps.ArchivedAtAsync(_app, pets);

            HttpResponseMessage response = await PersonalCategoryRequests.ArchiveAsync(user.Client, pets);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(firstArchivedAt, await PersonalCategorySteps.ArchivedAtAsync(_app, pets));
        }

        [Fact]
        public async Task Unarchive_OwnArchivedCategory_Answers204AndBringsItBackToTheList()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Pets");

            HttpResponseMessage response = await PersonalCategoryRequests.UnarchiveAsync(user.Client, pets);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await PersonalCategorySteps.ArchivedAtAsync(_app, pets));
            Assert.Equal(["Pets"], await PersonalCategorySteps.CustomNamesAsync(user.Client));
        }

        [Fact]
        public async Task Unarchive_OwnActiveCategory_Answers204AndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            string before = await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId);

            HttpResponseMessage response = await PersonalCategoryRequests.UnarchiveAsync(user.Client, pets);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId));
        }

        [Theory]
        [InlineData("Pets")]
        [InlineData("pets")]
        public async Task Unarchive_AnActiveOwnCategoryHasTheSameName_Answers400NameTakenAndKeepsItArchived(string activeName)
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid oldPets = await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Pets");
            await PersonalCategorySteps.CreateAsync(user.Client, activeName);
            string before = await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId);

            HttpResponseMessage response = await PersonalCategoryRequests.UnarchiveAsync(user.Client, oldPets);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_TAKEN);
            Assert.Equal(before, await PersonalCategorySteps.SnapshotOfOwnerAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Unarchive_AnotherArchivedOwnCategoryHasTheSameName_Answers204AndBringsBackOnlyTheOneAskedFor()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid first = await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Pets");
            Guid second = await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Pets");

            HttpResponseMessage response = await PersonalCategoryRequests.UnarchiveAsync(user.Client, first);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await PersonalCategorySteps.ArchivedAtAsync(_app, first));
            Assert.NotNull(await PersonalCategorySteps.ArchivedAtAsync(_app, second));
        }

        [Theory]
        [MemberData(nameof(ArchiveEndpoints))]
        public async Task ArchiveOrUnarchive_ABuiltInCategory_Answers404CategoryNotFoundAndChangesNothing(string endpoint)
        {
            SignedInUser user = await _app.SignUpAsync();
            string before = await PersonalCategorySteps.SnapshotOfAllAsync(_app);

            HttpResponseMessage response = await PersonalCategoryRequests.SendAsync(user.Client, endpoint, BuiltInFoodId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
            Assert.Equal(before, await PersonalCategorySteps.SnapshotOfAllAsync(_app));
        }

        [Theory]
        [MemberData(nameof(ArchiveEndpoints))]
        public async Task ArchiveOrUnarchive_UnknownCategory_Answers404CategoryNotFound(string endpoint)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalCategoryRequests.SendAsync(user.Client, endpoint, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
        }

        public static TheoryData<string> ArchiveEndpoints => ["archive", "unarchive"];

        private Task SetArchivedAtAsync(Guid categoryId, DateTimeOffset archivedAt)
        {
            return _app.ExecuteAsync(
                "UPDATE categories SET archived_at = @archived_at WHERE id = @id",
                new NpgsqlParameter("archived_at", archivedAt),
                new NpgsqlParameter("id", categoryId));
        }
    }
}
