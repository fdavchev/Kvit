using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public class JoinGuardTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(GuardedWrites.ClosingState, ResultCodes.GROUP_CLOSING)]
        [InlineData(GuardedWrites.FinishedState, ResultCodes.GROUP_FINISHED)]
        public async Task Join_ByANewPerson_Answers400AndAddsNoOne(string state, string expectedCode)
        {
            (SettlementGroup group, string inviteToken) = await CreateGroupInStateAsync(state);
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, inviteToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedCode);
            Assert.Equal(0, await GroupRows.CountMemberRowsOfUserAsync(_app, group.GroupId, newcomer.UserId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, newcomer.UserId, "JoinedViaInvite"));
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData(GuardedWrites.ClosingState, ResultCodes.GROUP_CLOSING)]
        [InlineData(GuardedWrites.FinishedState, ResultCodes.GROUP_FINISHED)]
        public async Task Join_ByANewPersonWhoClaimsAName_Answers400AndTheNameStaysUnclaimed(string state, string expectedCode)
        {
            (SettlementGroup group, string inviteToken) = await CreateGroupInStateAsync(state);
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, inviteToken, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedCode);
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.MarkoRowId, "user_id"));
        }

        [Theory]
        [InlineData(GuardedWrites.ClosingState)]
        [InlineData(GuardedWrites.FinishedState)]
        public async Task Join_ByACurrentMember_StillAnswers200WithTheGroupIdAndChangesNothing(string state)
        {
            (SettlementGroup group, string inviteToken) = await CreateGroupInStateAsync(state);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(group.Third.Client, inviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(group.GroupId, (await AuthRequests.ReadJsonAsync(response)).GetProperty("groupId").GetGuid());
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Join_ByAPersonWhoLeftBeforeTheGroupFinished_Answers400GroupFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            string inviteToken = (await GroupRows.GroupValueAsync(_app, group.GroupId, "invite_token"))!;
            await GuardSteps.AssertNoContentAsync(GroupRequests.LeaveAsync(group.Member.Client, group.GroupId));
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(group.Member.Client, inviteToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            Assert.Equal("Left", await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "end_kind"));
        }

        [Theory]
        [InlineData(GuardedWrites.ClosingState)]
        [InlineData(GuardedWrites.FinishedState)]
        public async Task Preview_OfAClosingOrFinishedGroup_StillAnswers200WithTheNameAndAnOpenInviteForANewPerson(string state)
        {
            (_, string inviteToken) = await CreateGroupInStateAsync(state);
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupRequests.PreviewAsync(newcomer.Client, inviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Open", body.GetProperty("status").GetString());
            Assert.Equal(GroupRows.DefaultName, body.GetProperty("name").GetString());
        }

        [Theory]
        [InlineData(GuardedWrites.ClosingState)]
        [InlineData(GuardedWrites.FinishedState)]
        public async Task Preview_OfAClosingOrFinishedGroup_StillAnswersAlreadyMemberToACurrentMember(string state)
        {
            (SettlementGroup group, string inviteToken) = await CreateGroupInStateAsync(state);

            HttpResponseMessage response = await GroupRequests.PreviewAsync(group.Member.Client, inviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("AlreadyMember", body.GetProperty("status").GetString());
            Assert.Equal(group.GroupId, body.GetProperty("groupId").GetGuid());
        }

        private async Task<(SettlementGroup Group, string InviteToken)> CreateGroupInStateAsync(string state)
        {
            SettlementGroup group = state == GuardedWrites.ClosingState
                ? await ClosingSteps.CreateClosingGroupAsync(_app)
                : await ClosingSteps.CreateFinishedGroupAsync(_app);
            string inviteToken = (await GroupRows.GroupValueAsync(_app, group.GroupId, "invite_token"))!;

            return (group, inviteToken);
        }
    }
}
