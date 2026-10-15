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
    public class FinishedGroupRefusesWritesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [MemberData(nameof(GuardedWrites.NamesCalledByMembersAndRefusedWhenFinished), MemberType = typeof(GuardedWrites))]
        public async Task Write_ByAMemberInAFinishedGroup_Answers400GroupFinishedAndChangesNothing(string name)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named(name);
            SendWrite send = await write.PrepareAsync(_app, group);
            await GuardStates.ForceFinishedAsync(_app, group.GroupId);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await send(group.Member.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(GuardedWrites.NamesCalledByTheOwnerAndRefusedWhenFinished), MemberType = typeof(GuardedWrites))]
        public async Task Write_ByTheOwnerInAFinishedGroup_Answers400GroupFinishedAndChangesNothing(string name)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named(name);
            SendWrite send = await write.PrepareAsync(_app, group);
            await GuardStates.ForceFinishedAsync(_app, group.GroupId);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await send(group.Owner.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task AddExpense_ByAMemberInAGroupThatFinishedByConfirmations_Answers400GroupFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, GuardSteps.MemberExpenseInput(group));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            Assert.Equal(ClosingSteps.ExpensesInTheSetup, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task EditExpense_ByTheOwnerInAGroupThatFinishedByConfirmations_Answers400GroupFinishedUntilTheOwnerUnlocks()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid expenseId = await GuardSteps.AddBalancedPairOfExpensesAsync(_app, group);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);
            ExpenseInput bigger = GuardSteps.MemberExpenseInput(group, GuardSteps.BiggerExpenseMinor);

            HttpResponseMessage refused = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, bigger);
            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);
            HttpResponseMessage afterUnlock = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, bigger);

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            await SettlementSteps.AssertNoContentAsync(afterUnlock);
        }

        [Fact]
        public async Task RecordPayment_ByAMemberInAGroupThatFinishedByConfirmations_Answers400GroupFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Member.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, ZeroBalanceScenario.ShareMinor, ExpenseInputs.Mkd);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            Assert.Equal(0, await SettlementRows.CountAsync(_app, group.GroupId));
        }
    }
}
