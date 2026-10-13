using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Balances
{
    public class BalancesReadOnlyTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_ChangesNothingInTheGroupItsExpensesItsEventsOrTheUsageLog()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);
            string expensesBefore = await ExpenseRows.SnapshotAsync(_app, group.GroupId);
            string groupBefore = await GroupRows.SnapshotAsync(_app, group.GroupId);
            string usageBefore = await UsageSnapshotAsync(group);

            await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            await BalanceRequests.ReadBalancedAsync(group.Member.Client, group.GroupId);

            Assert.Equal(expensesBefore, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(groupBefore, await GroupRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(usageBefore, await UsageSnapshotAsync(group));
        }

        [Fact]
        public async Task Get_KeepsTheRowCountsOfExpensesMembersAndEvents()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);
            int expensesBefore = await ExpenseRows.CountExpensesAsync(_app, group.GroupId);
            int membersBefore = await GroupRows.CountMembersAsync(_app, group.GroupId);
            int eventsBefore = await GroupRows.CountEventsAsync(_app, group.GroupId);

            await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(7, expensesBefore);
            Assert.Equal(4, membersBefore);
            Assert.Equal(expensesBefore, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
            Assert.Equal(membersBefore, await GroupRows.CountMembersAsync(_app, group.GroupId));
            Assert.Equal(eventsBefore, await GroupRows.CountEventsAsync(_app, group.GroupId));
        }

        private async Task<string> UsageSnapshotAsync(ExpenseGroup group)
        {
            List<string?> counts = await _app.QueryAsync(
                "SELECT count(*)::text FROM usage_events WHERE user_id = @owner OR user_id = @member",
                new NpgsqlParameter("owner", group.Owner.UserId),
                new NpgsqlParameter("member", group.Member.UserId));

            return Assert.Single(counts)!;
        }
    }
}
