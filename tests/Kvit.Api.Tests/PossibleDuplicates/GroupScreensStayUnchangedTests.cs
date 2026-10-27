using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public class GroupScreensStayUnchangedTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly string[] ForbiddenWords = ["possibleDuplicates", "mergedWithExpenseId", "uplicate"];

        [Fact]
        public async Task GroupScreens_PersonWithAMergedEntry_HaveNoDuplicateFieldInAnyAnswer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);

            HttpResponseMessage groupAnswer = await GroupRequests.GetAsync(group.Member.Client, group.GroupId);
            HttpResponseMessage groupList = await GroupRequests.ListAsync(group.Member.Client);
            HttpResponseMessage members = await GroupRequests.ListMembersAsync(group.Member.Client, group.GroupId);
            HttpResponseMessage expenseList = await ExpenseRequests.ListAsync(group.Member.Client, group.GroupId);
            HttpResponseMessage expense = await ExpenseRequests.GetAsync(group.Member.Client, group.GroupId, expenseId);
            HttpResponseMessage activity = await ExpenseRequests.ActivityAsync(group.Member.Client, group.GroupId);

            foreach (HttpResponseMessage response in new[] { groupAnswer, groupList, members, expenseList, expense, activity })
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string body = (await AuthRequests.ReadJsonAsync(response)).GetRawText();
                foreach (string word in ForbiddenWords)
                {
                    Assert.DoesNotContain(word, body, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        [Fact]
        public async Task Activity_Answering_AddsNoLineToTheGroupsFeed()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            JsonElement before = await AuthRequests.ReadJsonAsync(await ExpenseRequests.ActivityAsync(group.Owner.Client, group.GroupId));

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);

            JsonElement after = await AuthRequests.ReadJsonAsync(await ExpenseRequests.ActivityAsync(group.Owner.Client, group.GroupId));
            Assert.Equal(before.GetRawText(), after.GetRawText());
        }
    }
}
