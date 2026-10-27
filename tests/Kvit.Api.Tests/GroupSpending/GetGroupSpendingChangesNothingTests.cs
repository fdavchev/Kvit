using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.GroupSpending
{
    public class GetGroupSpendingChangesNothingTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly string[] ForbiddenWords = ["\"plan\"", "\"spending\"", "\"categories\"", "\"totalMinor\""];

        [Fact]
        public async Task Get_ChangesNothingInTheGroupItsExpensesItsEventsOrTheUsageLog()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, group, 1_000, ExpenseInputs.Eur);
            string expensesBefore = await ExpenseRows.SnapshotAsync(_app, group.GroupId);
            string groupBefore = await GroupRows.SnapshotAsync(_app, group.GroupId);
            string usageBefore = await UsageSnapshotAsync(group);

            await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);
            await GroupSpendingRequests.ReadAsync(group.Member.Client, group.GroupId);

            Assert.Equal(expensesBefore, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(groupBefore, await GroupRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(usageBefore, await UsageSnapshotAsync(group));
        }

        [Fact]
        public async Task Get_TwiceInARow_AnswersTheSameText()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, group, 1_000, ExpenseInputs.Eur);

            string first = await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId);
            string second = await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(first, second);
        }

        [Fact]
        public async Task GroupScreens_AfterTheSpendingAnswer_GainNoSpendingOrPlanField()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            HttpResponseMessage groupAnswer = await GroupRequests.GetAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage groupList = await GroupRequests.ListAsync(group.Owner.Client);
            HttpResponseMessage members = await GroupRequests.ListMembersAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage expenseList = await ExpenseRequests.ListAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage expense = await ExpenseRequests.GetAsync(group.Owner.Client, group.GroupId, expenseId);
            HttpResponseMessage activity = await ExpenseRequests.ActivityAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage balances = await BalanceRequests.GetAsync(group.Owner.Client, group.GroupId);

            foreach (HttpResponseMessage response in new[] { groupAnswer, groupList, members, expenseList, expense, activity, balances })
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string body = (await AuthRequests.ReadJsonAsync(response)).GetRawText();
                foreach (string word in ForbiddenWords)
                {
                    Assert.DoesNotContain(word, body, StringComparison.Ordinal);
                }
            }
        }

        [Fact]
        public async Task GetGroup_AfterTheSpendingAnswer_StillHasExactlyTheSixteenGroupFields()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            HttpResponseMessage response = await GroupRequests.GetAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(GroupRequests.GroupFieldNames, GroupSpendingJson.PropertyNamesOf(await AuthRequests.ReadJsonAsync(response)));
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
