using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public class StartClosingTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Start_ByTheOwnerWhenEveryoneIsKvit_Answers204WithNoBody()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Start_ByTheOwner_SetsClosingWithTheClockTimeAndLeavesFinishedAtEmpty()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await ClosingRows.ClosingStartedAtUtcAsync(_app, group.GroupId));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "finished_at"));
        }

        [Fact]
        public async Task Start_ByTheOwner_StoresNoResponseYetAndNoGroupFinishedEvent()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
        }

        [Fact]
        public async Task Start_OwnerWithOneMemberWhoHasAnAccount_WaitsForThatMemberInsteadOfFinishing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await ClosingSteps.AddCountedExpenseAsync(_app, setup.GroupId, setup.Owner, setup.OwnerRowId);

            await ClosingSteps.StartAsync(setup.Owner, setup.GroupId);

            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, setup.GroupId));
            Assert.Null(await GroupRows.GroupValueAsync(_app, setup.GroupId, "finished_at"));
        }

        [Fact]
        public async Task Start_AfterTheDebtWasPaidInFull_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Start_AfterAPendingPaymentWasConfirmed_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Theory]
        [InlineData(ZeroBalanceScenario.RejectedStatus)]
        [InlineData(ZeroBalanceScenario.CancelledStatus)]
        public async Task Start_WhenTheOnlyPaymentWasRejectedOrCancelled_Answers204BecauseNothingIsPendingAnyMore(string status)
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, status);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Start_WhenAnExpenseWasDeletedAgain_Answers204()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            Guid expenseId = await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await SettlementSteps.AssertNoContentAsync(await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId));

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Start_ChangesOnlyTheStatusAndTheClosingTime()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            string nameBefore = (await GroupRows.GroupValueAsync(_app, group.GroupId, "name"))!;
            string tokenBefore = (await GroupRows.GroupValueAsync(_app, group.GroupId, "invite_token"))!;
            int membersBefore = await GroupRows.CountMembersAsync(_app, group.GroupId);

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            Assert.Equal(nameBefore, await GroupRows.GroupValueAsync(_app, group.GroupId, "name"));
            Assert.Equal(tokenBefore, await GroupRows.GroupValueAsync(_app, group.GroupId, "invite_token"));
            Assert.Equal(group.Owner.UserId.ToString(), await GroupRows.GroupValueAsync(_app, group.GroupId, "owner_user_id"));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "deleted_at"));
            Assert.Equal(membersBefore, await GroupRows.CountMembersAsync(_app, group.GroupId));
        }
    }
}
