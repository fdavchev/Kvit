using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Guards;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public class StartClosingNoExpensesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Start_OnABrandNewGroupWithNoExpense_Answers400GroupNoExpensesAndWritesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NO_EXPENSES);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Null(await ClosingRows.ClosingStartedAtUtcAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingStarted"));
            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Start_OnAGroupJustCreatedWithTheOwnerAlone_Answers400GroupNoExpensesInsteadOfFinishingAtOnce()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);

            HttpResponseMessage response = await ClosingRequests.StartAsync(owner.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NO_EXPENSES);
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, groupId));
            Assert.Null(await ClosingRows.FinishedAtUtcAsync(_app, groupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, groupId, "GroupFinished"));
        }

        [Fact]
        public async Task Start_OwnerWithOnlyPlainNamesAndNoExpense_Answers400GroupNoExpensesInsteadOfFinishingAtOnce()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);
            await GroupRows.InsertMemberAsync(_app, groupId, null, "Marko", owner.UserId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(owner.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NO_EXPENSES);
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, groupId));
        }

        [Fact]
        public async Task Start_WhenTheOnlyExpenseWasDeleted_Answers400GroupNoExpensesAndWritesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid expenseId = await AddKvitExpenseAsync(group);
            await SettlementSteps.AssertNoContentAsync(await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId));
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NO_EXPENSES);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Start_AfterTheDeletedExpenseWasRestored_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid expenseId = await AddKvitExpenseAsync(group);
            await SettlementSteps.AssertNoContentAsync(await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId));
            await SettlementSteps.AssertNoContentAsync(await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId));

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Start_WhenOneOfTwoExpensesWasDeleted_Answers204BecauseTheOtherStillCounts()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            Guid secondExpenseId = await AddKvitExpenseAsync(group);
            await SettlementSteps.AssertNoContentAsync(await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, secondExpenseId));

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Start_AfterTheFirstExpenseWasAdded_Answers204WhereItWasRefusedBefore()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            HttpResponseMessage refused = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);
            await AddKvitExpenseAsync(group);

            HttpResponseMessage accepted = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.GROUP_NO_EXPENSES);
            await SettlementSteps.AssertNoContentAsync(accepted);
        }

        [Fact]
        public async Task Start_WhenOnlyConfirmedPaymentsExistAndEveryBalanceIsZero_Answers400GroupNoExpensesBecauseAPaymentIsNotAnExpense()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NO_EXPENSES);
        }

        [Fact]
        public async Task Start_WhenThereIsNoExpenseAndAPaymentIsPending_Answers400GroupNoExpensesBeforeGroupNotKvit()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NO_EXPENSES);
        }

        [Fact]
        public async Task Start_WhenThereIsNoExpenseAndAConfirmedPaymentLeavesABalance_Answers400GroupNoExpensesBeforeGroupNotKvit()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NO_EXPENSES);
        }

        [Fact]
        public async Task Start_WhenTheOnlyExpenseBelongsToAnotherGroup_Answers400GroupNoExpenses()
        {
            SettlementGroup emptyGroup = await SettlementSteps.CreateGroupAsync(_app);
            await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);

            HttpResponseMessage response = await ClosingRequests.StartAsync(emptyGroup.Owner.Client, emptyGroup.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NO_EXPENSES);
        }

        [Fact]
        public async Task Start_ByAMemberWhoIsNotTheOwnerOfAGroupWithNoExpense_Answers403GroupNotOwnerBeforeTheExpensesAreLookedFor()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Start_ByAStrangerOnAGroupWithNoExpense_Answers404GroupNotFoundBeforeTheExpensesAreLookedFor()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await ClosingRequests.StartAsync(stranger.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Theory]
        [InlineData(GuardedWrites.ClosingState)]
        [InlineData(GuardedWrites.FinishedState)]
        public async Task Start_OnAGroupThatIsNotOpenAndHasNoExpense_Answers400GroupNotOpenBeforeTheExpensesAreLookedFor(string state)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            if (state == GuardedWrites.ClosingState)
            {
                await GuardStates.ForceClosingAsync(_app, group.GroupId);
            }
            else
            {
                await GuardStates.ForceFinishedAsync(_app, group.GroupId);
            }

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_OPEN);
        }

        [Fact]
        public async Task Start_OnAOneBillWithNoExpense_Answers400GroupIsOneBillBeforeTheExpensesAreLookedFor()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await ClosingSteps.CreateEmptyOneBillAsync(_app, owner);

            HttpResponseMessage response = await ClosingRequests.StartAsync(owner.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_IS_ONE_BILL);
        }

        private async Task<Guid> AddKvitExpenseAsync(SettlementGroup group)
        {
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);

            return await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
        }
    }
}
