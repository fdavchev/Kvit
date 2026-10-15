using System.Net;
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
    public class ExpiredClosingWriteGuardTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int JustInsideTheWindowSeconds = (24 * 60 * 60) - 1;
        private const int ExactlyTheWindowSeconds = 24 * 60 * 60;
        private const int TwentyFiveHoursSeconds = 25 * 60 * 60;

        [Theory]
        [InlineData(ExactlyTheWindowSeconds)]
        [InlineData(TwentyFiveHoursSeconds)]
        public async Task AddExpense_ByAMemberAfter24Hours_Answers400GroupFinishedAndSavesTheFinish(int secondsSinceStart)
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            DateTimeOffset startedAt = _app.Clock.GetUtcNow();
            _app.Clock.Advance(TimeSpan.FromSeconds(secondsSinceStart));

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, GuardSteps.MemberExpenseInput(group));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            Assert.Equal(ClosingSteps.ExpensesInTheSetup, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
            await AssertFinishedAtTheEndOfTheWindowAsync(group, startedAt);
        }

        [Fact]
        public async Task AddExpense_ByTheOwnerAfter24Hours_Answers400GroupFinishedAndDoesNotCancelTheClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            DateTimeOffset startedAt = _app.Clock.GetUtcNow();
            _app.Clock.Advance(TimeSpan.FromSeconds(TwentyFiveHoursSeconds));

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, GuardSteps.OwnerExpenseInput(group));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            Assert.Equal(ClosingSteps.ExpensesInTheSetup, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingCancelled"));
            await AssertFinishedAtTheEndOfTheWindowAsync(group, startedAt);
        }

        [Fact]
        public async Task UpdateGroup_ByTheOwnerAfter24Hours_Answers400GroupFinishedAndSavesTheFinish()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            DateTimeOffset startedAt = _app.Clock.GetUtcNow();
            _app.Clock.Advance(TimeSpan.FromSeconds(TwentyFiveHoursSeconds));

            HttpResponseMessage response = await GroupRequests.UpdateAsync(group.Owner.Client, group.GroupId, GuardSteps.RenamedGroup);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            Assert.Equal(GroupRows.DefaultName, await GroupRows.GroupValueAsync(_app, group.GroupId, "name"));
            await AssertFinishedAtTheEndOfTheWindowAsync(group, startedAt);
        }

        [Fact]
        public async Task Join_ByANewPersonAfter24Hours_Answers400GroupFinishedAndSavesTheFinish()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            DateTimeOffset startedAt = _app.Clock.GetUtcNow();
            string inviteToken = (await GroupRows.GroupValueAsync(_app, group.GroupId, "invite_token"))!;
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            _app.Clock.Advance(TimeSpan.FromSeconds(TwentyFiveHoursSeconds));

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, inviteToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            Assert.Equal(0, await GroupRows.CountMemberRowsOfUserAsync(_app, group.GroupId, newcomer.UserId));
            await AssertFinishedAtTheEndOfTheWindowAsync(group, startedAt);
        }

        [Fact]
        public async Task Leave_ByAMemberAfter24Hours_SucceedsAndSavesTheFinish()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            DateTimeOffset startedAt = _app.Clock.GetUtcNow();
            _app.Clock.Advance(TimeSpan.FromSeconds(TwentyFiveHoursSeconds));

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Left", await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "end_kind"));
            await AssertFinishedAtTheEndOfTheWindowAsync(group, startedAt);
        }

        [Fact]
        public async Task AddExpense_ByTheOwnerOneSecondBefore24Hours_SucceedsAndCancelsTheClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(JustInsideTheWindowSeconds));

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, GuardSteps.OwnerExpenseInput(group));

            await GuardSteps.AssertSucceededAsync(response);
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingCancelled"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
        }

        [Fact]
        public async Task AddExpense_ByAMemberOneSecondBefore24Hours_Answers400GroupClosingAndTheGroupIsNotFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(JustInsideTheWindowSeconds));

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, GuardSteps.MemberExpenseInput(group));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_CLOSING);
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
        }

        private async Task AssertFinishedAtTheEndOfTheWindowAsync(SettlementGroup group, DateTimeOffset startedAt)
        {
            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(SettlementRows.UtcTextOf(startedAt + ClosingSteps.ClosingWindow), await ClosingRows.FinishedAtUtcAsync(_app, group.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
            Assert.Equal(group.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "GroupFinished", "actor_user_id"));
        }
    }
}
