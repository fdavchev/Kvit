using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public class AnswerDuplicateOneMergeTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string SecondDate = "2026-10-04";

        [Fact]
        public async Task Put_SameForAnEntryAlreadyMergedWithAnotherExpense_Answers400AlreadyMergedAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.SameAnswer);
            string before = await DuplicateAnswerRows.SnapshotAsync(_app);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, secondExpense, DuplicateAnswerRequests.SameAnswer);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, DuplicateAnswerRequests.AlreadyMergedCode);
            Assert.Equal(before, await DuplicateAnswerRows.SnapshotAsync(_app));
            Assert.Equal(firstExpense, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId));
        }

        [Fact]
        public async Task Put_SameForAnExpenseAlreadyMergedWithAnotherEntry_Answers400AlreadyMergedAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid firstEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid secondEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, firstEntry, expenseId, DuplicateAnswerRequests.SameAnswer);
            string before = await DuplicateAnswerRows.SnapshotAsync(_app);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, secondEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, DuplicateAnswerRequests.AlreadyMergedCode);
            Assert.Equal(before, await DuplicateAnswerRows.SnapshotAsync(_app));
            Assert.Equal(expenseId, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, firstEntry));
            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, secondEntry));
        }

        [Fact]
        public async Task Put_SameAgainForTheMergedPair_Answers204AndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid otherExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, otherExpense, DuplicateAnswerRequests.NewAnswer);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);
            string before = await DuplicateAnswerRows.SnapshotAsync(_app);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await DuplicateAnswerRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Put_NewAfterSameForTheMergedEntryAndAnotherExpense_Answers204AndStoresIt()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.SameAnswer);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, secondExpense, DuplicateAnswerRequests.NewAnswer);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(DuplicateAnswerRequests.NewAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, secondExpense));
            Assert.Equal(firstExpense, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId));
        }

        [Fact]
        public async Task Put_NewForAnExpenseMergedWithAnotherEntry_Answers204AndStoresIt()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid firstEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid secondEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, firstEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, secondEntry, expenseId, DuplicateAnswerRequests.NewAnswer);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(DuplicateAnswerRequests.NewAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, secondEntry, expenseId));
        }

        [Fact]
        public async Task Put_NewToSameForAnEntryThatIsMergedWithAnotherExpense_Answers400AlreadyMergedAndKeepsTheNew()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, secondExpense, DuplicateAnswerRequests.NewAnswer);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.SameAnswer);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, secondExpense, DuplicateAnswerRequests.SameAnswer);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, DuplicateAnswerRequests.AlreadyMergedCode);
            Assert.Equal(DuplicateAnswerRequests.NewAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, secondExpense));
        }

        [Fact]
        public async Task Put_NewToSameForAnExpenseThatIsMergedWithAnotherEntry_Answers400AlreadyMergedAndKeepsTheNew()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid firstEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid secondEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, secondEntry, expenseId, DuplicateAnswerRequests.NewAnswer);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, firstEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, secondEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, DuplicateAnswerRequests.AlreadyMergedCode);
            Assert.Equal(DuplicateAnswerRequests.NewAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, secondEntry, expenseId));
        }

        [Fact]
        public async Task Put_SameForAnotherExpenseAfterTheMergeWasChangedToNew_Answers204()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.SameAnswer);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.NewAnswer);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, secondExpense, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(secondExpense, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId));
        }

        [Fact]
        public async Task Put_SameForAnotherExpenseAfterTheMergeWasRemoved_Answers204()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.SameAnswer);
            await DuplicateAnswerRequests.DeleteAsync(group.Member.Client, entryId, firstExpense);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, secondExpense, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(secondExpense, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId));
        }

        [Fact]
        public async Task Put_SameWhenTheEarlierMergedExpenseWasDeleted_Answers204AndMergesWithTheNewOne()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.SameAnswer);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, firstExpense);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, secondExpense, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(secondExpense, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId));
        }

        [Fact]
        public async Task Put_SameWhenTheGroupOfTheEarlierMergedExpenseWasDeleted_Answers204AndMergesWithTheNewOne()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            (_, Guid secondExpense) = await PossibleDuplicateSteps.AddSharedExpenseInAnotherGroupAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.SameAnswer);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 1);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, secondExpense, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(secondExpense, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId));
        }

        [Fact]
        public async Task Put_SameWhenTheEarlierMergedEntryWasDeleted_Answers204AndMergesTheNewEntry()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid firstEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid secondEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, firstEntry, expenseId, DuplicateAnswerRequests.SameAnswer);
            await PersonalEntryRequests.DeleteAsync(group.Member.Client, firstEntry);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, secondEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(expenseId, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, secondEntry));
        }

        [Fact]
        public async Task Put_SameForAnExpenseWhoseMergeCameBackWhenItWasRestored_Answers400AlreadyMerged()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid firstEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid secondEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, firstEntry, expenseId, DuplicateAnswerRequests.SameAnswer);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, secondEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, DuplicateAnswerRequests.AlreadyMergedCode);
        }

        [Fact]
        public async Task Put_SameByAnotherPersonForTheSameExpense_Answers204BecauseOnlyYourOwnMergesCount()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid memberEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid ownerEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Owner);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, memberEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Owner.Client, ownerEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(expenseId, await PossibleDuplicateSteps.MergedWithAsync(group.Owner.Client, ownerEntry));
            Assert.Equal(expenseId, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, memberEntry));
        }

        [Fact]
        public async Task Put_SameForAnEntryWhoseOtherAnswersAreOnlyNew_Answers204()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.NewAnswer);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, secondExpense, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(secondExpense, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId));
        }

        [Fact]
        public async Task Put_UnknownEntryForAnExpenseThatIsAlreadyMerged_Answers404EntryNotFoundFirst()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid mergedEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid deletedEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, mergedEntry, expenseId, DuplicateAnswerRequests.SameAnswer);
            await PersonalEntryRequests.DeleteAsync(group.Member.Client, deletedEntry);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, deletedEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }

        [Fact]
        public async Task Put_UnknownExpenseForAnEntryThatIsAlreadyMerged_Answers404ExpenseNotFoundBeforeAlreadyMerged()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid deletedExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, deletedExpense);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, deletedExpense, DuplicateAnswerRequests.SameAnswer);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
        }

        [Fact]
        public async Task Put_InvalidAnswerForAnEntryThatIsAlreadyMerged_Answers400AnswerInvalidBeforeAlreadyMerged()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.SameAnswer);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, secondExpense, "Maybe");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, DuplicateAnswerRequests.InvalidAnswerCode);
        }
    }
}
