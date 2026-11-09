using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Guards;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsClosedGroupTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        [Theory]
        [MemberData(nameof(ClosedGroupWrites.NamesCalledByMembers), MemberType = typeof(ClosedGroupWrites))]
        public async Task Write_ByAMemberInAClosedGroup_Answers403GroupClosedByAdminAndChangesNothing(string name)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SendWrite send = await GuardedWrites.Named(name).PrepareAsync(_app, group);
            await CloseAsync(group.GroupId);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await send(group.Member.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(ClosedGroupWrites.NamesCalledByTheOwner), MemberType = typeof(ClosedGroupWrites))]
        public async Task Write_ByTheOwnerInAClosedGroup_Answers403GroupClosedByAdminAndChangesNothing(string name)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SendWrite send = await GuardedWrites.Named(name).PrepareAsync(_app, group);
            await CloseAsync(group.GroupId);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await send(group.Owner.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task RestoreGroup_ByTheOwnerOfADeletedAndThenClosedGroup_Answers403GroupClosedByAdminAndLeavesItDeleted()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.AssertNoContentAsync(await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId));
            await OwnerToolsRows.SetAdminClosedAtAsync(_app, group.GroupId, _app.Clock.GetUtcNow());

            HttpResponseMessage response = await GroupRequests.RestoreAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
            Assert.NotNull(await GroupRows.GroupValueAsync(_app, group.GroupId, "deleted_at"));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task CountsInBudget_SwitchedInAClosedGroup_Answers403GroupClosedByAdminAndChangesNothing(bool callerIsOwner, bool countsInBudget)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await CloseAsync(group.GroupId);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);
            HttpClient caller = callerIsOwner ? group.Owner.Client : group.Member.Client;

            HttpResponseMessage response = await BudgetShareRequests.SetAsync(caller, group.GroupId, countsInBudget);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData(GuardedWrites.ClosingState)]
        [InlineData(GuardedWrites.FinishedState)]
        public async Task AddExpense_ByAMemberInAClosedGroupThatIsAlsoClosingOrFinished_AnswersTheAdminCodeNotTheOtherOne(string state)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named("AddExpense");
            SendWrite send = await write.PrepareAsync(_app, group);
            await GuardStates.ApplyAsync(_app, group, write, state);
            await CloseAsync(group.GroupId);

            HttpResponseMessage response = await send(group.Member.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
        }

        [Theory]
        [InlineData(GuardedWrites.ClosingState)]
        [InlineData(GuardedWrites.FinishedState)]
        public async Task UpdateGroup_ByTheOwnerOfAClosedGroupThatIsAlsoClosingOrFinished_AnswersTheAdminCodeNotTheOtherOne(string state)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named("UpdateGroup");
            SendWrite send = await write.PrepareAsync(_app, group);
            await GuardStates.ApplyAsync(_app, group, write, state);
            await CloseAsync(group.GroupId);

            HttpResponseMessage response = await send(group.Owner.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
        }

        [Fact]
        public async Task Join_ByANewPersonInAClosedGroup_Answers403GroupClosedByAdminAndAddsNoOne()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            string inviteToken = await GuestSteps.InviteTokenOfAsync(_app, group.GroupId);
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            await CloseAsync(group.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, inviteToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
            Assert.Equal(0, await GroupRows.CountMemberRowsOfUserAsync(_app, group.GroupId, newcomer.UserId));
        }

        [Fact]
        public async Task Join_ByANewPersonWhoClaimsAFreeNameInAClosedGroup_Answers403AndTheNameStaysUnclaimed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            string inviteToken = await GuestSteps.InviteTokenOfAsync(_app, group.GroupId);
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            await CloseAsync(group.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, inviteToken, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.MarkoRowId, "user_id"));
        }

        [Fact]
        public async Task JoinAsGuest_InAClosedGroup_Answers403GroupClosedByAdminAndAddsNoMember()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            string inviteToken = await GuestSteps.InviteTokenOfAsync(_app, group.GroupId);
            await CloseAsync(group.GroupId);
            int membersBefore = await GroupRows.CountMembersAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsGuestAsync(_app.CreateClient(), inviteToken, name: "Baba Mara");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
            Assert.Equal(membersBefore, await GroupRows.CountMembersAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Join_ByACurrentMemberOfAClosedGroup_StillAnswers200AndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            string inviteToken = await GuestSteps.InviteTokenOfAsync(_app, group.GroupId);
            await CloseAsync(group.GroupId);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(group.Third.Client, inviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task StartClosing_ByTheOwnerOfAClosedGroup_Answers403GroupClosedByAdminAndStartsNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            await CloseAsync(group.GroupId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task ConfirmClosing_ByAMemberOfAClosedGroupThatIsClosing_Answers403GroupClosedByAdmin()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await CloseAsync(group.GroupId);

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
        }

        [Fact]
        public async Task ObjectToClosing_ByAMemberOfAClosedGroupThatIsClosing_Answers403GroupClosedByAdmin()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await CloseAsync(group.GroupId);

            HttpResponseMessage response = await ClosingRequests.ObjectAsync(group.Member.Client, group.GroupId, "Not right");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
        }

        [Fact]
        public async Task Unlock_ByTheOwnerOfAGroupThatFinishedAndWasThenClosed_Answers403GroupClosedByAdminAndStaysFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            await CloseAsync(group.GroupId);

            HttpResponseMessage response = await ClosingRequests.UnlockAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Unlock_ByTheOwnerOfAnOpenClosedGroup_Answers403GroupClosedByAdminNotNotFinished()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await CloseAsync(group.GroupId);

            HttpResponseMessage response = await ClosingRequests.UnlockAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
        }

        [Fact]
        public async Task Unlock_ByTheOwnerAfterTheOwnerOfKvitReopened_Succeeds()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            await CloseAsync(group.GroupId);
            await OwnerToolsSteps.ReopenGroupAsync(admin, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.UnlockAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(ClosedGroupWrites.ReadNames), MemberType = typeof(ClosedGroupWrites))]
        public async Task Read_ByAMemberOrTheOwnerOfAClosedGroup_StillAnswers200(string name)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await CloseAsync(group.GroupId);
            string path = ClosedGroupWrites.ReadPathOf(name, group.GroupId);

            HttpResponseMessage byMember = await group.Member.Client.GetAsync(path, TestContext.Current.CancellationToken);
            HttpResponseMessage byOwner = await group.Owner.Client.GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, byMember.StatusCode);
            Assert.Equal(HttpStatusCode.OK, byOwner.StatusCode);
        }

        [Fact]
        public async Task Leave_ByAMemberWithAZeroBalanceInAClosedGroup_Answers204AndEndsTheirRowAsLeft()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await CloseAsync(group.GroupId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Left", await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "end_kind"));
            Assert.NotNull(await OwnerToolsRows.AdminClosedAtAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Leave_ByAMemberWhoOwesMoneyInAClosedGroup_StillAnswers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await CloseAsync(group.GroupId);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Write_AfterTheOwnerOfKvitReopenedTheGroup_Succeeds()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await CloseAsync(group.GroupId);
            await ProblemResponse.AssertAsync(
                await GroupRequests.UpdateAsync(group.Owner.Client, group.GroupId, "Renamed trip"),
                HttpStatusCode.Forbidden,
                OwnerToolsRequests.GroupClosedByAdminCode);

            await OwnerToolsSteps.ReopenGroupAsync(admin, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(await GroupRequests.UpdateAsync(group.Owner.Client, group.GroupId, "Renamed trip"));
            Assert.Equal("Renamed trip", await GroupRows.GroupValueAsync(_app, group.GroupId, "name"));
            HttpResponseMessage added = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, GuardSteps.MemberExpenseInput(group));
            Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        }

        [Fact]
        public async Task Write_InAGroupClosedForSomeoneElse_StillWorksInAnotherGroup()
        {
            SettlementGroup closed = await SettlementSteps.CreateGroupAsync(_app);
            SettlementGroup other = await SettlementSteps.CreateGroupAsync(_app);
            await CloseAsync(closed.GroupId);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(other.Owner.Client, other.GroupId, "Renamed trip");

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Write_ByAnAdminWhoIsAlsoTheOwnerOfTheClosedGroup_IsRefusedLikeAnyoneElse()
        {
            SignedInUser admin = await _app.SignInAdminAsync();
            Guid groupId = await _app.CreateGroupAsync(admin);
            await OwnerToolsSteps.CloseGroupAsync(admin, groupId);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(admin.Client, groupId, "Renamed trip");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.GroupClosedByAdminCode);
        }

        private async Task CloseAsync(Guid groupId)
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            await OwnerToolsSteps.CloseGroupAsync(admin, groupId);
        }
    }
}
