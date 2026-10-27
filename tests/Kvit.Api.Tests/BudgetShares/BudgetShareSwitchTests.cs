using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.BudgetShares
{
    public class BudgetShareSwitchTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Default_AccountRowsOfTheOwnerAndOfSomeoneWhoJoins_CountInTheBudget()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            HttpResponseMessage joined = await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken);
            Guid newcomerRowId = await GroupRows.CurrentMemberIdAsync(_app, setup.GroupId, newcomer.UserId);

            Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, setup.OwnerRowId));
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, newcomerRowId));
        }

        [Fact]
        public async Task Default_PlainNameAddedByTheOwner_CountsInTheBudget()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage added = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, "Marko");

            Assert.True(added.IsSuccessStatusCode);
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, await _app.MemberRowIdByNameAsync(setup.GroupId, "Marko")));
        }

        [Fact]
        public async Task Default_MemberRowInsertedWithoutTheColumn_CountsInTheBudget()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, setup.MemberRowId));
        }

        [Fact]
        public async Task Set_Off_Answers204WithNoBodyAndTurnsTheCallersRowOff()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await BudgetShareRequests.SetAsync(setup.Member.Client, setup.GroupId, false);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.False(await BudgetShareSteps.MemberCountsAsync(_app, setup.MemberRowId));
        }

        [Fact]
        public async Task Set_OffThenOn_TurnsTheCallersRowBackOn()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);

            HttpResponseMessage response = await BudgetShareRequests.SetAsync(setup.Member.Client, setup.GroupId, true);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, setup.MemberRowId));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Set_TheSameValueTwice_Answers204BothTimesAndKeepsTheValue(bool countsInBudget)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage first = await BudgetShareRequests.SetAsync(setup.Member.Client, setup.GroupId, countsInBudget);
            HttpResponseMessage second = await BudgetShareRequests.SetAsync(setup.Member.Client, setup.GroupId, countsInBudget);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
            Assert.Equal(countsInBudget, await BudgetShareSteps.MemberCountsAsync(_app, setup.MemberRowId));
        }

        [Fact]
        public async Task Set_Off_LeavesEveryOtherRowOfTheGroupAlone()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);

            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);

            Assert.False(await BudgetShareSteps.MemberCountsAsync(_app, setup.MemberRowId));
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, setup.OwnerRowId));
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, plainId));
        }

        [Fact]
        public async Task Set_ByTheOwner_LeavesTheMembersSwitchAlone()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);

            await BudgetShareSteps.SetAsync(setup.Owner, setup.GroupId, false);
            await BudgetShareSteps.SetAsync(setup.Owner, setup.GroupId, true);

            Assert.False(await BudgetShareSteps.MemberCountsAsync(_app, setup.MemberRowId));
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, setup.OwnerRowId));
        }

        [Fact]
        public async Task Set_Off_LeavesTheSwitchOfTheSamePersonInAnotherGroupAlone()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(setup.Owner);
            Guid memberRowInOtherGroup = await GroupRows.InsertMemberAsync(_app, otherGroupId, setup.Member.UserId, "Bojan", setup.Owner.UserId);

            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);

            Assert.False(await BudgetShareSteps.MemberCountsAsync(_app, setup.MemberRowId));
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, memberRowInOtherGroup));
        }

        [Fact]
        public async Task Set_ByAPersonWhoClaimedAName_ChangesTheClaimedRowAndNotTheRowSetAside()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            HttpResponseMessage claimed = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);
            Assert.Equal(HttpStatusCode.NoContent, claimed.StatusCode);

            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);

            Assert.False(await BudgetShareSteps.MemberCountsAsync(_app, plainId));
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, setup.MemberRowId));
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, setup.OwnerRowId));
        }

        [Fact]
        public async Task Set_Off_WritesNoActivityOrUsageEvent()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            int groupEventsBefore = await GroupRows.CountEventsAsync(_app, setup.GroupId);
            int memberEventsBefore = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, setup.Member.UserId);

            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);
            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, true);

            Assert.Equal(groupEventsBefore, await GroupRows.CountEventsAsync(_app, setup.GroupId));
            Assert.Equal(memberEventsBefore, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, setup.Member.UserId));
        }

        [Fact]
        public async Task Set_ByANonMember_Answers404GroupNotFoundAndChangesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await BudgetShareRequests.SetAsync(stranger.Client, setup.GroupId, false);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        public async Task Set_ByAMemberWhoLeftOrWasRemoved_Answers404GroupNotFoundAndChangesNothing(string endKind)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, endKind);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await BudgetShareRequests.SetAsync(setup.Member.Client, setup.GroupId, false);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Set_InAnUnknownGroup_Answers404GroupNotFoundWhileAnonymousAnswers401()
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpClient anonymous = _app.CreateClient();
            Guid unknownGroupId = Guid.CreateVersion7();

            HttpResponseMessage routed = await BudgetShareRequests.SetAsync(user.Client, unknownGroupId, false);
            HttpResponseMessage response = await BudgetShareRequests.SetAsync(anonymous, unknownGroupId, false);

            await ProblemResponse.AssertAsync(routed, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Set_InADeletedGroup_Answers404GroupNotFoundEvenForTheOwner()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.SetDeletedAsync(_app, setup.GroupId, setup.Owner.UserId, 0);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await BudgetShareRequests.SetAsync(setup.Owner.Client, setup.GroupId, false);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Set_ByAGuest_Answers403GuestNotAllowedAndChangesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, setup.GroupId, "Gost");
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await BudgetShareRequests.SetAsync(guest.Client, setup.GroupId, false);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, guest.RowId));
        }

        [Theory]
        [InlineData("Finished", ResultCodes.GROUP_FINISHED, false)]
        [InlineData("Finished", ResultCodes.GROUP_FINISHED, true)]
        [InlineData("Closing", ResultCodes.GROUP_CLOSING, false)]
        public async Task Set_InAFinishedGroupOrByAMemberOfAClosingGroup_Answers400AndChangesNothing(string status, string expectedCode, bool callerIsOwner)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.SetStatusAsync(_app, setup.GroupId, status);
            SignedInUser caller = callerIsOwner ? setup.Owner : setup.Member;
            Guid callerRowId = callerIsOwner ? setup.OwnerRowId : setup.MemberRowId;
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await BudgetShareRequests.SetAsync(caller.Client, setup.GroupId, false);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedCode);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, callerRowId));
        }

        [Fact]
        public async Task Set_ByTheOwnerOfAClosingGroup_Answers204AndTheClosingKeepsRunning()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.SetStatusAsync(_app, setup.GroupId, "Closing");

            HttpResponseMessage response = await BudgetShareRequests.SetAsync(setup.Owner.Client, setup.GroupId, false);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(await BudgetShareSteps.MemberCountsAsync(_app, setup.OwnerRowId));
            Assert.Equal("Closing", await GroupRows.GroupValueAsync(_app, setup.GroupId, "status"));
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"countsInBudget\":null}")]
        [InlineData("{\"countsInBudget\":\"yes\"}")]
        [InlineData("{\"countsInBudget\":1}")]
        [InlineData("not json")]
        [InlineData("")]
        public async Task Set_BodyWithoutABoolean_Answers400AndKeepsTheSwitchAsItWas(string json)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await BudgetShareRequests.SetWithJsonTextAsync(setup.Member.Client, setup.GroupId, json);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True(await BudgetShareSteps.MemberCountsAsync(_app, setup.MemberRowId));
        }
    }
}
