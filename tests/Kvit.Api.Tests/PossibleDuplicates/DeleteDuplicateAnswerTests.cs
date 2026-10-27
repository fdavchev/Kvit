using System.Net;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public class DeleteDuplicateAnswerTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(DuplicateAnswerRequests.SameAnswer)]
        [InlineData(DuplicateAnswerRequests.NewAnswer)]
        public async Task Delete_ExistingAnswer_Answers204WithNoBodyRemovesTheRowAndBringsThePairBack(string answer)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, answer);
            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));

            HttpResponseMessage response = await DuplicateAnswerRequests.DeleteAsync(group.Member.Client, entryId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Null(await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, expenseId));
            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, expenseId)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Delete_AnswerSame_UnmergesTheEntrySoItCountsAgain()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);
            Assert.Equal(0, await PossibleDuplicateSteps.MonthSpendingTotalAsync(group.Member.Client));

            await DuplicateAnswerRequests.DeleteAsync(group.Member.Client, entryId, expenseId);

            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId));
            Assert.Equal(PossibleDuplicateSteps.ShareMinor, await PossibleDuplicateSteps.MonthSpendingTotalAsync(group.Member.Client));
        }

        [Fact]
        public async Task Delete_SameAnswerTwice_Answers204BothTimesAndLeavesNoRow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);

            HttpResponseMessage first = await DuplicateAnswerRequests.DeleteAsync(group.Member.Client, entryId, expenseId);
            HttpResponseMessage second = await DuplicateAnswerRequests.DeleteAsync(group.Member.Client, entryId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Delete_AnswerThatNeverExisted_Answers204AndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid answeredEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Owner);
            await PossibleDuplicateSteps.AnswerAsync(group.Owner, answeredEntry, expenseId, DuplicateAnswerRequests.NewAnswer);
            string before = await DuplicateAnswerRows.SnapshotAsync(_app);

            HttpResponseMessage response = await DuplicateAnswerRequests.DeleteAsync(group.Member.Client, entryId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await DuplicateAnswerRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Delete_UnknownEntryAndExpense_Answers204AndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            string before = await DuplicateAnswerRows.SnapshotAsync(_app);

            HttpResponseMessage response = await DuplicateAnswerRequests.DeleteAsync(user.Client, Guid.CreateVersion7(), Guid.CreateVersion7());

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await DuplicateAnswerRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Delete_AnswerOfAnotherPerson_Answers204AndLeavesThatAnswerAlone()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid ownerEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Owner);
            await PossibleDuplicateSteps.AnswerAsync(group.Owner, ownerEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            HttpResponseMessage response = await DuplicateAnswerRequests.DeleteAsync(group.Member.Client, ownerEntry, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Owner.UserId, ownerEntry, expenseId));
            Assert.Equal(expenseId, await PossibleDuplicateSteps.MergedWithAsync(group.Owner.Client, ownerEntry));
        }

        [Fact]
        public async Task Delete_OnlyTheNamedPair_LeavesTheOtherAnswersOfTheSameEntry()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-10-04");
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.NewAnswer);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, secondExpense, DuplicateAnswerRequests.NewAnswer);

            await DuplicateAnswerRequests.DeleteAsync(group.Member.Client, entryId, firstExpense);

            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, firstExpense)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
            Assert.Equal(DuplicateAnswerRequests.NewAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, secondExpense));
        }

        [Fact]
        public async Task Delete_AnswerOfADeletedEntry_StillAnswers204()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);
            await PersonalEntryRequests.DeleteAsync(group.Member.Client, entryId);

            HttpResponseMessage response = await DuplicateAnswerRequests.DeleteAsync(group.Member.Client, entryId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
