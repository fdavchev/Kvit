using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class PersonalEntryAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [MemberData(nameof(PersonalEntryRequests.AllEndpoints), MemberType = typeof(PersonalEntryRequests))]
        public async Task Anonymous_Answers401(string endpoint)
        {
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await PersonalEntryRequests.SendAsync(anonymous, endpoint, Guid.CreateVersion7());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(PersonalEntryRequests.AllEndpoints), MemberType = typeof(PersonalEntryRequests))]
        public async Task Guest_Answers403GuestNotAllowedAndWritesNothing(string endpoint)
        {
            GuestMember guest = await JoinAsGuestAsync();
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.SendAsync(guest.Client, endpoint, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Theory]
        [MemberData(nameof(PersonalEntryRequests.EndpointsWithAnId), MemberType = typeof(PersonalEntryRequests))]
        public async Task AnotherUser_EntryOfSomeoneElse_Answers404NotFoundAndChangesNothing(string endpoint)
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid entryId = await PersonalEntrySteps.AddAsync(owner.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.SendAsync(other.Client, endpoint, entryId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task AnotherUser_RestoreOfADeletedEntryOfSomeoneElse_Answers404NotFoundAndKeepsItDeleted()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid entryId = await PersonalEntrySteps.AddAndDeleteAsync(owner.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(other.Client, entryId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            Assert.NotNull(await PersonalEntrySteps.ValueAsync(_app, entryId, "deleted_at"));
        }

        private async Task<GuestMember> JoinAsGuestAsync()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);

            return await GuestSteps.JoinWithNewNameAsync(_app, groupId, "Gost");
        }
    }
}
