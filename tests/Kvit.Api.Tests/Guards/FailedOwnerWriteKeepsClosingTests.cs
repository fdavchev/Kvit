using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public class FailedOwnerWriteKeepsClosingTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task AddExpense_WithAnAmountOfZero_Answers400AndTheClosingStays()
        {
            (SettlementGroup group, _) = await CreateClosingGroupWithExpensesAsync();
            ClosingFacts before = await ClosingFacts.ReadAsync(_app, group.GroupId);
            ExpenseInput zero = GuardSteps.OwnerExpenseInput(group, 0);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, zero);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE);
            Assert.Equal(before, await ClosingFacts.ReadAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task EditExpense_WithAnAmountOfZero_Answers400AndTheClosingStays()
        {
            (SettlementGroup group, Guid expenseId) = await CreateClosingGroupWithExpensesAsync();
            ClosingFacts before = await ClosingFacts.ReadAsync(_app, group.GroupId);
            ExpenseInput zero = GuardSteps.MemberExpenseInput(group, 0);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, zero);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE);
            Assert.Equal(before, await ClosingFacts.ReadAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task RestoreExpense_ThatIsNotDeleted_Answers400AndTheClosingStays()
        {
            (SettlementGroup group, Guid expenseId) = await CreateClosingGroupWithExpensesAsync();
            ClosingFacts before = await ClosingFacts.ReadAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_NOT_DELETED);
            Assert.Equal(before, await ClosingFacts.ReadAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task RecordPayment_WithAnAmountOfZero_Answers400AndTheClosingStays()
        {
            (SettlementGroup group, _) = await CreateClosingGroupWithExpensesAsync();
            ClosingFacts before = await ClosingFacts.ReadAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 0, ExpenseInputs.Mkd);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_AMOUNT_NOT_POSITIVE);
            Assert.Equal(before, await ClosingFacts.ReadAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task MakeOwner_OfTheOwnerThemself_Answers400AndTheClosingStays()
        {
            (SettlementGroup group, _) = await CreateClosingGroupWithExpensesAsync();
            ClosingFacts before = await ClosingFacts.ReadAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.MakeOwnerAsync(group.Owner.Client, group.GroupId, group.OwnerRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_ALREADY_OWNER);
            Assert.Equal(before, await ClosingFacts.ReadAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task RemoveMember_ThatIsTheOwner_Answers400AndTheClosingStays()
        {
            (SettlementGroup group, _) = await CreateClosingGroupWithExpensesAsync();
            ClosingFacts before = await ClosingFacts.ReadAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.OwnerRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_IS_OWNER);
            Assert.Equal(before, await ClosingFacts.ReadAsync(_app, group.GroupId));
        }

        private async Task<(SettlementGroup Group, Guid MemberExpenseId)> CreateClosingGroupWithExpensesAsync()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid memberExpenseId = await GuardSteps.AddBalancedPairOfExpensesAsync(_app, group);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);

            return (group, memberExpenseId);
        }
    }
}
