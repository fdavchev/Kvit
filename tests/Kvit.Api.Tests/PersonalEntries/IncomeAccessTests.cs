using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class IncomeAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        public static TheoryData<string> IncomeEndpoints => ["add", "edit", "delete", "restore"];

        public static TheoryData<string> IncomeEndpointsWithAnId => ["edit", "delete", "restore"];

        [Fact]
        public async Task Anonymous_AddIncome_Answers401()
        {
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(anonymous, IncomeEntryInput.Of());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Anonymous_EditIncome_Answers401()
        {
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(anonymous, Guid.CreateVersion7(), IncomeEntryInput.Of());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(IncomeEndpoints))]
        public async Task Guest_Answers403GuestNotAllowedAndWritesNothing(string endpoint)
        {
            GuestMember guest = await JoinAsGuestAsync();
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await SendAsync(guest.Client, endpoint, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Theory]
        [MemberData(nameof(IncomeEndpointsWithAnId))]
        public async Task AnotherUser_IncomeEntryOfSomeoneElse_Answers404NotFoundAndChangesNothing(string endpoint)
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(owner.Client, IncomeEntryInput.Of());
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await SendAsync(other.Client, endpoint, entryId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task AnotherUser_RestoreOfADeletedIncomeOfSomeoneElse_Answers404NotFoundAndKeepsItDeleted()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid entryId = await PersonalEntrySteps.AddIncomeAndDeleteAsync(owner.Client, IncomeEntryInput.Of());

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(other.Client, entryId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            Assert.NotNull(await PersonalEntrySteps.ValueAsync(_app, entryId, "deleted_at"));
        }

        private static Task<HttpResponseMessage> SendAsync(HttpClient client, string endpoint, Guid entryId)
        {
            IncomeEntryInput input = IncomeEntryInput.Of();

            return endpoint switch
            {
                "add" => PersonalEntryRequests.AddIncomeAsync(client, input),
                "edit" => PersonalEntryRequests.EditIncomeAsync(client, entryId, input with { AmountMinor = 5_000_000 }),
                "delete" => PersonalEntryRequests.DeleteAsync(client, entryId),
                "restore" => PersonalEntryRequests.RestoreAsync(client, entryId),
                _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Not an income endpoint."),
            };
        }

        private async Task<GuestMember> JoinAsGuestAsync()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);

            return await GuestSteps.JoinWithNewNameAsync(_app, groupId, "Gost");
        }
    }
}
