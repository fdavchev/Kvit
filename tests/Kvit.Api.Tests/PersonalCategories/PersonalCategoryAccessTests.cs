using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalCategories
{
    public class PersonalCategoryAccessTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Theory]
        [MemberData(nameof(PersonalCategoryRequests.AllEndpoints), MemberType = typeof(PersonalCategoryRequests))]
        public async Task Anonymous_Answers401(string endpoint)
        {
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await PersonalCategoryRequests.SendAsync(anonymous, endpoint, Guid.CreateVersion7());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(PersonalCategoryRequests.AllEndpoints), MemberType = typeof(PersonalCategoryRequests))]
        public async Task Guest_Answers403GuestNotAllowedAndWritesNothing(string endpoint)
        {
            GuestMember guest = await JoinAsGuestAsync();
            string before = await PersonalCategorySteps.SnapshotOfAllAsync(_app);

            HttpResponseMessage response = await PersonalCategoryRequests.SendAsync(guest.Client, endpoint, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.Equal(before, await PersonalCategorySteps.SnapshotOfAllAsync(_app));
        }

        [Theory]
        [MemberData(nameof(PersonalCategoryRequests.EndpointsWithAnId), MemberType = typeof(PersonalCategoryRequests))]
        public async Task Guest_WithTheIdOfABuiltInCategory_Answers403GuestNotAllowed(string endpoint)
        {
            GuestMember guest = await JoinAsGuestAsync();

            HttpResponseMessage response = await PersonalCategoryRequests.SendAsync(guest.Client, endpoint, Guid.Parse("c47e9a10-5f3b-4d2e-9a61-000000000001"));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
        }

        [Theory]
        [MemberData(nameof(PersonalCategoryRequests.EndpointsWithAnId), MemberType = typeof(PersonalCategoryRequests))]
        public async Task AnotherUser_ActiveCategoryOfSomeoneElse_Answers404CategoryNotFoundAndChangesNothing(string endpoint)
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid pets = await PersonalCategorySteps.CreateAsync(owner.Client, "Pets");
            string before = await PersonalCategorySteps.SnapshotOfAllAsync(_app);

            HttpResponseMessage response = await PersonalCategoryRequests.SendAsync(other.Client, endpoint, pets);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
            Assert.Equal(before, await PersonalCategorySteps.SnapshotOfAllAsync(_app));
        }

        [Theory]
        [MemberData(nameof(PersonalCategoryRequests.EndpointsWithAnId), MemberType = typeof(PersonalCategoryRequests))]
        public async Task AnotherUser_ArchivedCategoryOfSomeoneElse_Answers404CategoryNotFoundAndChangesNothing(string endpoint)
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid pets = await PersonalCategorySteps.CreateArchivedAsync(owner.Client, "Pets");
            string before = await PersonalCategorySteps.SnapshotOfAllAsync(_app);

            HttpResponseMessage response = await PersonalCategoryRequests.SendAsync(other.Client, endpoint, pets);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
            Assert.Equal(before, await PersonalCategorySteps.SnapshotOfAllAsync(_app));
        }

        [Fact]
        public async Task AnotherUser_UpdateWithTheNameOfTheOwnersCategory_Answers404NotNameTaken()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid pets = await PersonalCategorySteps.CreateAsync(owner.Client, "Pets");
            await PersonalCategorySteps.CreateAsync(owner.Client, "Kids");

            HttpResponseMessage response = await PersonalCategoryRequests.UpdateAsync(other.Client, pets, "Kids");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
        }

        private async Task<GuestMember> JoinAsGuestAsync()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);

            return await GuestSteps.JoinWithNewNameAsync(_app, groupId, "Gost");
        }
    }
}
