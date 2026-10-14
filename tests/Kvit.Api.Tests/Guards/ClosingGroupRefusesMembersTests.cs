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
    public class ClosingGroupRefusesMembersTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [MemberData(nameof(GuardedWrites.NamesCalledByMembers), MemberType = typeof(GuardedWrites))]
        public async Task Write_ByAMemberWhileTheGroupIsClosing_Answers400GroupClosingAndChangesNothing(string name)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named(name);
            SendWrite send = await write.PrepareAsync(_app, group);
            await GuardStates.CloseAsync(_app, group, write);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);
            ClosingFacts closingBefore = await ClosingFacts.ReadAsync(_app, group.GroupId);

            HttpResponseMessage response = await send(group.Member.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_CLOSING);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(closingBefore, await ClosingFacts.ReadAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task AddExpense_ByAMemberWhoHasNotAnsweredTheClosing_Answers400GroupClosingAndStoresNoExpense()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Third.Client, group.GroupId, GuardSteps.MemberExpenseInput(group));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_CLOSING);
            Assert.Equal(0, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Leave_ByAMemberWhoHasNotAnsweredTheClosing_Answers400GroupClosingAndTheyStayInTheGroup()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Third.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_CLOSING);
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.ThirdRowId, "removed_at"));
        }
    }
}
