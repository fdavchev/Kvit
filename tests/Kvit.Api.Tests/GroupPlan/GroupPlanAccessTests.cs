using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public class GroupPlanAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string StoredBefore = "200000:MKD";

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task Anonymous_Answers401WhileTheSameRouteExistsForASignedInUser(string change)
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage routed = await GroupPlanRequests.ChangeAsync(user.Client, change, Guid.CreateVersion7());
            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(anonymous, change, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(routed, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task Anonymous_ForAnExistingGroup_Answers401AndChangesNothing(string change)
        {
            ExpenseGroup group = await PlannedGroupAsync();
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(anonymous, change, group.GroupId, 1_000, GroupPlanRequests.Eur);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task NonMember_Answers404GroupNotFoundAndChangesNothing(string change)
        {
            ExpenseGroup group = await PlannedGroupAsync();
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(stranger.Client, change, group.GroupId, 1_000, GroupPlanRequests.Eur);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task MemberOfAnotherGroupOnly_Answers404GroupNotFound(string change)
        {
            ExpenseGroup group = await PlannedGroupAsync();
            ExpenseGroup other = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(other.Owner.Client, change, group.GroupId, 1_000, GroupPlanRequests.Eur);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task GroupThatDoesNotExist_Answers404GroupNotFound(string change)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(user.Client, change, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task GroupIdThatIsNotAGuid_Answers404(string change)
        {
            SignedInUser user = await _app.SignUpAsync();
            string method = change == "set" ? "PUT" : "DELETE";

            HttpResponseMessage response = await GroupRequests.SendAsync(user.Client, method, "/api/groups/not-a-guid/plan", new { amountMinor = 100_000, currency = "MKD" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task DeletedGroup_Answers404GroupNotFoundEvenForTheOwner(string change)
        {
            ExpenseGroup group = await PlannedGroupAsync();
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(group.Owner.Client, change, group.GroupId, 1_000, GroupPlanRequests.Eur);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("set", "Removed")]
        [InlineData("remove", "Removed")]
        [InlineData("set", "Left")]
        [InlineData("remove", "Left")]
        public async Task FormerMember_Answers404GroupNotFound(string change, string endKind)
        {
            ExpenseGroup group = await PlannedGroupAsync();
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(group.Member.Client, change, group.GroupId, 1_000, GroupPlanRequests.Eur);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task NonOwnerMember_Answers403GroupNotOwnerAndChangesNothing(string change)
        {
            ExpenseGroup group = await PlannedGroupAsync();
            int eventsBefore = await GroupRows.CountEventsAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(group.Member.Client, change, group.GroupId, 1_000, GroupPlanRequests.Eur);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
            Assert.Equal(eventsBefore, await GroupRows.CountEventsAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task Guest_ThatTookAFreeName_Answers403GroupNotOwnerAndChangesNothing(string change)
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(guest.Client, change, group.GroupId, 1_000, GroupPlanRequests.Eur);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task Guest_ThatJoinedWithANewName_Answers403GroupNotOwner(string change)
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, groupId, "Gost");

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(guest.Client, change, groupId, 1_000, GroupPlanRequests.Eur);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(GroupPlanRows.NoPlan, await GroupPlanRows.PlanOfAsync(_app, groupId));
        }

        [Fact]
        public async Task Put_NonMemberWithAnInvalidAmount_Answers404BeforeTheAmountIsChecked()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(stranger.Client, group.GroupId, 0, "USD");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Put_NonOwnerMemberWithAnInvalidAmount_Answers403BeforeTheAmountIsChecked()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(group.Member.Client, group.GroupId, 0, "USD");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task ClosingGroup_Owner_Answers204AndTheClosingKeepsRunning(string change)
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(group.Owner.Client, change, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Closing", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingCancelled"));
        }

        [Fact]
        public async Task ClosingGroup_OwnerSetsAPlan_StoresItAndKeepsTheOthersConfirmations()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            Assert.Equal("60000:EUR", await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
            Assert.Equal("Closing", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingConfirmed"));
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task ClosingGroup_NonOwnerMember_Answers403GroupNotOwnerAndChangesNothing(string change)
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(group.Member.Client, change, group.GroupId, 1_000, GroupPlanRequests.Eur);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
            Assert.Equal("Closing", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task FinishedGroup_Owner_Answers400GroupFinishedAndChangesNothing(string change)
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(group.Owner.Client, change, group.GroupId, 1_000, GroupPlanRequests.Eur);

            Assert.Equal("Finished", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("set")]
        [InlineData("remove")]
        public async Task FinishedGroup_NonOwnerMember_Answers403GroupNotOwnerBeforeTheState(string change)
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);

            HttpResponseMessage response = await GroupPlanRequests.ChangeAsync(group.Member.Client, change, group.GroupId, 1_000, GroupPlanRequests.Eur);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Put_OwnerOfAFinishedGroupWithAnInvalidAmount_Answers400GroupFinishedBeforeTheAmountIsChecked()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, 0, "USD");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
        }

        [Fact]
        public async Task Put_OwnerOfAClosingGroupWhoseWindowRanOut_Answers400GroupFinishedAndKeepsThePlan()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            _app.Clock.Advance(ClosingSteps.ClosingWindow + ClosingSteps.OneMinute);

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, 1_000, GroupPlanRequests.Eur);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PATCH")]
        public async Task OtherMethods_Answer405AndTheStoredPlanStaysTheSame(string method)
        {
            ExpenseGroup group = await PlannedGroupAsync();

            HttpResponseMessage response = await GroupRequests.SendAsync(group.Owner.Client, method, GroupPlanRequests.PathOf(group.GroupId), new { amountMinor = 1_000, currency = "EUR" });

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        private async Task<ExpenseGroup> PlannedGroupAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            return group;
        }
    }
}
