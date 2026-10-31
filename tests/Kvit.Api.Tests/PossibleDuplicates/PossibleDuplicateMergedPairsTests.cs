using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public class PossibleDuplicateMergedPairsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string SecondDate = "2026-10-04";

        [Fact]
        public async Task Month_EntryMergedWithOneExpense_IsNotListedWithAnyOtherExpense()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Assert.Equal(2, (await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client)).Count);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.SameAnswer);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_ExpenseMergedWithOneEntry_IsNotListedWithAnyOtherEntry()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid firstEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Assert.Equal(2, (await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client)).Count);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, firstEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_ExpenseMergedWithAnEntryOfAnotherMonth_IsNotListedWithAnEntryOfThisMonth()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-10-01");
            Guid septemberEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member, entryDate: "2026-09-29");
            await PossibleDuplicateSteps.AddEntryAsync(group.Member, entryDate: "2026-10-02");
            Assert.Single(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
            await PossibleDuplicateSteps.AnswerAsync(group.Member, septemberEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_MergedPair_LeavesPairsOfOtherEntriesAndOtherExpensesListed()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid mergedExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid mergedEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid otherExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-10-01", PossibleDuplicateSteps.WholeMinor * 3);
            Guid otherEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member, PossibleDuplicateSteps.WholeMinor * 3 / 2, "2026-10-01");

            await PossibleDuplicateSteps.AnswerAsync(group.Member, mergedEntry, mergedExpense, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(PossibleDuplicateSteps.Keys((otherEntry, otherExpense)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_EntryAndExpenseOnlyAnsweredNewForOtherPartners_AreStillListedWithEachOther()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid firstEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid secondEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, firstEntry, firstExpense, DuplicateAnswerRequests.NewAnswer);

            Assert.Equal(
                PossibleDuplicateSteps.Keys((firstEntry, secondExpense), (secondEntry, firstExpense), (secondEntry, secondExpense)),
                await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_MergedExpenseDeleted_TheEntryAndTheOtherCandidatesComeBack()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid mergedExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid otherExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, mergedExpense, DuplicateAnswerRequests.SameAnswer);
            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));

            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, mergedExpense);

            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, otherExpense)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_MergedExpenseDeletedAndRestored_TheMergeHidesTheOtherCandidatesAgain()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid mergedExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, mergedExpense, DuplicateAnswerRequests.SameAnswer);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, mergedExpense);
            Assert.Single(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));

            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, mergedExpense);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_MergedEntryDeleted_TheExpenseIsListedWithTheOtherEntriesAgain()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid mergedEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid otherEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, mergedEntry, expenseId, DuplicateAnswerRequests.SameAnswer);
            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));

            await PersonalEntryRequests.DeleteAsync(group.Member.Client, mergedEntry);

            Assert.Equal(PossibleDuplicateSteps.Keys((otherEntry, expenseId)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_MergedExpenseInADeletedGroup_TheEntryComesBackWithTheExpensesOfOtherGroups()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid mergedExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            (_, Guid otherExpense) = await PossibleDuplicateSteps.AddSharedExpenseInAnotherGroupAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, mergedExpense, DuplicateAnswerRequests.SameAnswer);
            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));

            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 1);

            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, otherExpense)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_MergeOfAnotherPerson_DoesNotHideMyPairs()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid memberEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid ownerEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Owner);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, memberEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(PossibleDuplicateSteps.Keys((ownerEntry, expenseId)), await PossibleDuplicateSteps.PairKeysAsync(group.Owner.Client));
        }
    }
}
