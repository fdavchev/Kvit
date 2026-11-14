using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsCloseGroupTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        [Fact]
        public async Task Close_OpenGroup_Answers204AndSetsAdminClosedAtToTheClockNow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await OwnerToolsRequests.CloseAsync(admin.Client, setup.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(_app.Clock.GetUtcNow(), await OwnerToolsRows.AdminClosedAtAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Close_OpenGroup_LeavesItsStatusOpen()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId);

            Assert.Equal("Open", await GroupRows.GroupValueAsync(_app, setup.GroupId, "status"));
        }

        [Fact]
        public async Task Close_Group_WritesOneLogRowWithTheGroupNameTheTrimmedReasonAndTheTime()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId, "  Hateful content  ");

            StoredAction row = Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, setup.GroupId));
            Assert.Equal(new StoredAction(admin.UserId, "GroupClosed", null, setup.GroupId, GroupRows.DefaultName, "Hateful content", _app.Clock.GetUtcNow()), row);
        }

        [Fact]
        public async Task Close_GroupThatIsAlreadyClosed_Answers204KeepsTheFirstTimeAndWritesNoSecondRow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId);
            DateTimeOffset firstTime = _app.Clock.GetUtcNow();
            _app.Clock.Advance(TimeSpan.FromMinutes(5));

            HttpResponseMessage response = await OwnerToolsRequests.CloseAsync(admin.Client, setup.GroupId, "A second reason");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(firstTime, await OwnerToolsRows.AdminClosedAtAsync(_app, setup.GroupId));
            Assert.Equal(OwnerToolsRequests.DefaultReason, Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, setup.GroupId)).Reason);
        }

        [Fact]
        public async Task Reopen_ClosedGroup_Answers204ClearsTheMarkAndWritesOneRow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId);
            _app.Clock.Advance(TimeSpan.FromMinutes(5));

            HttpResponseMessage response = await OwnerToolsRequests.ReopenAsync(admin.Client, setup.GroupId, " Sorted out ");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await OwnerToolsRows.AdminClosedAtAsync(_app, setup.GroupId));
            List<StoredAction> rows = await OwnerToolsRows.ActionsAboutAsync(_app, setup.GroupId);
            Assert.Equal(2, rows.Count);
            Assert.Equal(new StoredAction(admin.UserId, "GroupReopened", null, setup.GroupId, GroupRows.DefaultName, "Sorted out", _app.Clock.GetUtcNow()), rows[1]);
        }

        [Fact]
        public async Task Reopen_GroupThatIsNotClosed_Answers204AndWritesNoRow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await OwnerToolsRequests.ReopenAsync(admin.Client, setup.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await OwnerToolsRows.AdminClosedAtAsync(_app, setup.GroupId));
            Assert.Empty(await OwnerToolsRows.ActionsAboutAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Reopen_TwiceInARow_WritesOnlyOneReopenedRow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId);
            await OwnerToolsSteps.ReopenGroupAsync(admin, setup.GroupId);

            HttpResponseMessage response = await OwnerToolsRequests.ReopenAsync(admin.Client, setup.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(["GroupClosed", "GroupReopened"], (await OwnerToolsRows.ActionsAboutAsync(_app, setup.GroupId)).Select(row => row.Action));
        }

        [Fact]
        public async Task CloseReopenClose_WritesThreeRowsInOrderAndLeavesTheGroupClosed()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId, "first");
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.ReopenGroupAsync(admin, setup.GroupId, "second");
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId, "third");

            List<StoredAction> rows = await OwnerToolsRows.ActionsAboutAsync(_app, setup.GroupId);
            Assert.Equal(["GroupClosed", "GroupReopened", "GroupClosed"], rows.Select(row => row.Action));
            Assert.Equal(["first", "second", "third"], rows.Select(row => row.Reason));
            Assert.Equal(_app.Clock.GetUtcNow(), await OwnerToolsRows.AdminClosedAtAsync(_app, setup.GroupId));
        }

        [Theory]
        [MemberData(nameof(OwnerToolsCalls.GroupNames), MemberType = typeof(OwnerToolsCalls))]
        public async Task Action_OnAGroupThatDoesNotExist_Answers404GroupNotFoundAndWritesNoRow(string name)
        {
            OwnerToolsCall call = OwnerToolsCalls.Named(name);
            SignedInUser admin = await _app.SharedAdminAsync();
            int logRowsBefore = await OwnerToolsRows.CountActionsAsync(_app);

            HttpResponseMessage response = await call.SendAsync(admin.Client, Guid.NewGuid(), Guid.NewGuid(), OwnerToolsRequests.DefaultReason);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, OwnerToolsRequests.GroupNotFoundCode);
            Assert.Equal(logRowsBefore, await OwnerToolsRows.CountActionsAsync(_app));
        }

        [Fact]
        public async Task Close_GroupWithExpensesAndPayments_ChangesNoExpensePaymentOrMember()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);
            string expensesBefore = await ExpenseRows.SnapshotAsync(_app, group.GroupId);
            string paymentsBefore = await SettlementRows.SnapshotAsync(_app, group.GroupId);
            int membersBefore = await GroupRows.CountMembersAsync(_app, group.GroupId);

            await OwnerToolsSteps.CloseGroupAsync(admin, group.GroupId);

            Assert.Equal(expensesBefore, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(paymentsBefore, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(membersBefore, await GroupRows.CountMembersAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Close_Group_TouchesNoOtherGroup()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup closed = await _app.CreateGroupWithMemberAsync();
            GroupSetup bystander = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, bystander.GroupId);

            await OwnerToolsSteps.CloseGroupAsync(admin, closed.GroupId);

            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, bystander.GroupId));
            Assert.Null(await OwnerToolsRows.AdminClosedAtAsync(_app, bystander.GroupId));
            Assert.Empty(await OwnerToolsRows.ActionsAboutAsync(_app, bystander.GroupId));
        }

        [Fact]
        public async Task Reopen_GroupThatFinishedByItself_StaysFinishedAndItsOwnerCanThenUnlockIt()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            await OwnerToolsSteps.CloseGroupAsync(admin, group.GroupId);

            await OwnerToolsSteps.ReopenGroupAsync(admin, group.GroupId);

            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal("Finished", (await OwnerToolsRequests.ReadGroupAsync(admin.Client, group.GroupId)).GetProperty("status").GetString());
            await ProblemResponse.AssertAsync(
                await GroupRequests.UpdateAsync(group.Owner.Client, group.GroupId, "Renamed trip"),
                HttpStatusCode.BadRequest,
                ResultCodes.GROUP_FINISHED);
            await SettlementSteps.AssertNoContentAsync(await ClosingRequests.UnlockAsync(group.Owner.Client, group.GroupId));
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Close_GroupThatIsClosing_KeepsItsClosingAndReopenLeavesTheClosingRunning()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            await OwnerToolsSteps.CloseGroupAsync(admin, group.GroupId);
            await OwnerToolsSteps.ReopenGroupAsync(admin, group.GroupId);

            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
            await SettlementSteps.AssertNoContentAsync(await ClosingRequests.ConfirmAsync(group.Member.Client, group.GroupId));
        }

        [Fact]
        public async Task Close_ByAnAdminWhoIsAMemberOfTheGroup_StillWorksAndLogsOneRow()
        {
            SignedInUser admin = await _app.SignInAdminAsync();
            Guid groupId = await _app.CreateGroupAsync(admin);

            await OwnerToolsSteps.CloseGroupAsync(admin, groupId);

            Assert.NotNull(await OwnerToolsRows.AdminClosedAtAsync(_app, groupId));
            Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, groupId));
        }
    }
}
