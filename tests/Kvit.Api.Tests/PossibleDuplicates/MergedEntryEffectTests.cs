using System.Text.Json;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.BudgetLimits;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.BudgetSummary;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public class MergedEntryEffectTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const long TransportMinor = 30_000;

        [Fact]
        public async Task Entries_NothingAnswered_EveryEntryHasMergedWithExpenseIdNull()
        {
            Scene scene = await StartSceneAsync();

            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.EntryId));
            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.TransportEntryId));
        }

        [Fact]
        public async Task Entries_EntryAnsweredSame_StaysInTheListAndPointsAtTheExpense()
        {
            Scene scene = await StartSceneAsync();

            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);

            List<Guid> listed = await PersonalEntrySteps.EntryIdsOfMonthAsync(scene.Group.Member.Client);
            Assert.Equal(2, listed.Count);
            Assert.Contains(scene.EntryId, listed);
            Assert.Contains(scene.TransportEntryId, listed);
            Assert.Equal(scene.ExpenseId, await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.EntryId));
            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.TransportEntryId));
        }

        [Fact]
        public async Task Totals_EntryAnsweredSame_IsLeftOutOfTheSpendingTotalsAndTheCategoryTotals()
        {
            Scene scene = await StartSceneAsync();
            Assert.Equal(PossibleDuplicateSteps.ShareMinor + TransportMinor, await PossibleDuplicateSteps.MonthSpendingTotalAsync(scene.Group.Member.Client));

            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);

            JsonElement body = await PersonalEntrySteps.MonthAsync(scene.Group.Member.Client);
            JsonElement mkd = Assert.Single(body.GetProperty("totals").EnumerateArray());
            Assert.Equal(TransportMinor, mkd.GetProperty("totalMinor").GetInt64());
            Assert.Equal(TransportMinor, mkd.GetProperty("categories").EnumerateArray().Sum(category => category.GetProperty("totalMinor").GetInt64()));
        }

        [Fact]
        public async Task Totals_EntryAnsweredSame_LeavesIncomeAndTheGroupShareCounting()
        {
            Scene scene = await StartSceneAsync();
            await PersonalEntrySteps.AddIncomeAsync(scene.Group.Member.Client, IncomeEntryInput.Of(1_000_000));

            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);

            JsonElement body = await PersonalEntrySteps.MonthAsync(scene.Group.Member.Client);
            Assert.Equal(1_000_000, Assert.Single(body.GetProperty("incomeTotals").EnumerateArray()).GetProperty("totalMinor").GetInt64());
            JsonElement share = Assert.Single(body.GetProperty("groupShares").EnumerateArray());
            Assert.Equal(scene.ExpenseId, share.GetProperty("expenseId").GetGuid());
            Assert.Equal(PossibleDuplicateSteps.ShareMinor, share.GetProperty("shareMinor").GetInt64());
        }

        [Fact]
        public async Task ConvertedTotals_EntryAnsweredSame_IsLeftOutOfSpendingAndLeftButTheShareStays()
        {
            Scene scene = await StartSceneAsync();
            await BudgetCurrencySteps.SetAsync(scene.Group.Member, BudgetCurrencyRequests.Mkd);
            await PersonalEntrySteps.AddIncomeAsync(scene.Group.Member.Client, IncomeEntryInput.Of(1_000_000));
            JsonElement before = await BudgetCurrencySteps.ConvertedTotalsAsync(scene.Group.Member.Client);
            BudgetCurrencySteps.AssertConvertedTotals(before, BudgetCurrencyRequests.Mkd, PossibleDuplicateSteps.ShareMinor + TransportMinor, 1_000_000, PossibleDuplicateSteps.ShareMinor, 1_000_000 - PossibleDuplicateSteps.ShareMinor - TransportMinor - PossibleDuplicateSteps.ShareMinor);

            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);

            JsonElement after = await BudgetCurrencySteps.ConvertedTotalsAsync(scene.Group.Member.Client);
            BudgetCurrencySteps.AssertConvertedTotals(after, BudgetCurrencyRequests.Mkd, TransportMinor, 1_000_000, PossibleDuplicateSteps.ShareMinor, 1_000_000 - TransportMinor - PossibleDuplicateSteps.ShareMinor);
        }

        [Fact]
        public async Task Limits_EntryAnsweredSame_IsLeftOutOfSpentAndOverForTheCategoryAndTheMonth()
        {
            Scene scene = await StartSceneAsync();
            await BudgetLimitSteps.SetCategoryAsync(scene.Group.Member, PossibleDuplicateSteps.Food, 60_000);
            await BudgetLimitSteps.SetMonthAsync(scene.Group.Member, 100_000);
            JsonElement before = await BudgetLimitSteps.LimitsAsync(scene.Group.Member.Client);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(before, PossibleDuplicateSteps.Food), PossibleDuplicateSteps.Food, 60_000, 100_000, 40_000, 0);
            BudgetLimitSteps.AssertRow(before.GetProperty("month"), null, 100_000, 130_000, 30_000, 0);

            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);

            JsonElement after = await BudgetLimitSteps.LimitsAsync(scene.Group.Member.Client);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(after, PossibleDuplicateSteps.Food), PossibleDuplicateSteps.Food, 60_000, 50_000, 0, 0);
            BudgetLimitSteps.AssertRow(after.GetProperty("month"), null, 100_000, 80_000, 0, 0);
        }

        [Fact]
        public async Task Limits_EuroEntryAnsweredSame_IsLeftOutOfTheIncludedEuro()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid entryId = await BudgetLimitSteps.AddSpendingAsync(_app, group.Member, BudgetLimitSteps.Food, 500, BudgetCurrencyRequests.Eur, 61.0000m);
            Guid expenseId = await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.0000m);
            await BudgetLimitSteps.SetCategoryAsync(group.Member, BudgetLimitSteps.Food, 800_000);
            JsonElement before = await BudgetLimitSteps.LimitsAsync(group.Member.Client);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(before, BudgetLimitSteps.Food), BudgetLimitSteps.Food, 800_000, 61_000, 0, 1_000);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);

            JsonElement after = await BudgetLimitSteps.LimitsAsync(group.Member.Client);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(after, BudgetLimitSteps.Food), BudgetLimitSteps.Food, 800_000, 30_500, 0, 500);
        }

        [Fact]
        public async Task Summary_EntriesAnsweredSameInBothMonths_AreLeftOutOfThisAndThePreviousMonth()
        {
            SummaryScene scene = await StartSummarySceneAsync();
            JsonElement before = await BudgetSummarySteps.SummaryAsync(scene.Group.Member.Client);
            BudgetSummarySteps.AssertCurrencyRow(before, BudgetCurrencyRequests.Mkd, 100_000, 100_000, 0, 0);

            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);
            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.PreviousEntryId, scene.PreviousExpenseId, DuplicateAnswerRequests.SameAnswer);

            JsonElement after = await BudgetSummarySteps.SummaryAsync(scene.Group.Member.Client);
            BudgetSummarySteps.AssertCurrencyRow(after, BudgetCurrencyRequests.Mkd, 50_000, 50_000, 0, 0);
            BudgetSummarySteps.AssertCategories(after, BudgetCurrencyRequests.Mkd, (PossibleDuplicateSteps.Food, 50_000, 50_000));
        }

        [Fact]
        public async Task Summary_EntriesAnsweredSame_AreLeftOutOfTheConvertedTotalsToo()
        {
            SummaryScene scene = await StartSummarySceneAsync();
            await BudgetCurrencySteps.SetAsync(scene.Group.Member, BudgetCurrencyRequests.Mkd);
            BudgetSummarySteps.AssertConvertedTotals(await BudgetSummarySteps.ConvertedTotalsAsync(scene.Group.Member.Client), BudgetCurrencyRequests.Mkd, 100_000, 100_000, 0, 0);

            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);
            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.PreviousEntryId, scene.PreviousExpenseId, DuplicateAnswerRequests.SameAnswer);

            BudgetSummarySteps.AssertConvertedTotals(await BudgetSummarySteps.ConvertedTotalsAsync(scene.Group.Member.Client), BudgetCurrencyRequests.Mkd, 50_000, 50_000, 0, 0);
        }

        [Fact]
        public async Task Merge_GroupExpenseDeleted_TheEntryCountsAgainAndPointsAtNothing()
        {
            Scene scene = await StartSceneAsync();
            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);

            await ExpenseRequests.DeleteAsync(scene.Group.Owner.Client, scene.Group.GroupId, scene.ExpenseId);

            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.EntryId));
            Assert.Equal(PossibleDuplicateSteps.ShareMinor + TransportMinor, await PossibleDuplicateSteps.MonthSpendingTotalAsync(scene.Group.Member.Client));
            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(scene.Group.Member.Client));
        }

        [Fact]
        public async Task Merge_GroupExpenseRestored_TheEntryIsMergedAgainWithoutAskingAgain()
        {
            Scene scene = await StartSceneAsync();
            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);
            await ExpenseRequests.DeleteAsync(scene.Group.Owner.Client, scene.Group.GroupId, scene.ExpenseId);

            await ExpenseRequests.RestoreAsync(scene.Group.Owner.Client, scene.Group.GroupId, scene.ExpenseId);

            Assert.Equal(scene.ExpenseId, await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.EntryId));
            Assert.Equal(TransportMinor, await PossibleDuplicateSteps.MonthSpendingTotalAsync(scene.Group.Member.Client));
            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(scene.Group.Member.Client));
        }

        [Fact]
        public async Task Merge_GroupDeleted_TheEntryCountsAgainAndPointsAtNothing()
        {
            Scene scene = await StartSceneAsync();
            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);

            await GroupRows.SetDeletedAsync(_app, scene.Group.GroupId, scene.Group.Owner.UserId, 1);

            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.EntryId));
            Assert.Equal(PossibleDuplicateSteps.ShareMinor + TransportMinor, await PossibleDuplicateSteps.MonthSpendingTotalAsync(scene.Group.Member.Client));
        }

        [Theory]
        [InlineData("Closing")]
        [InlineData("Finished")]
        public async Task Merge_GroupThatIsClosingOrFinished_StillLeavesTheEntryOutOfTheTotals(string status)
        {
            Scene scene = await StartSceneAsync();
            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);

            await GroupRows.SetStatusAsync(_app, scene.Group.GroupId, status);

            Assert.Equal(scene.ExpenseId, await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.EntryId));
            Assert.Equal(TransportMinor, await PossibleDuplicateSteps.MonthSpendingTotalAsync(scene.Group.Member.Client));
        }

        [Fact]
        public async Task Merge_AnswerNew_EntryCountsAsBeforeIsNotMergedAndThePairNeverComesBack()
        {
            Scene scene = await StartSceneAsync();

            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.NewAnswer);

            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.EntryId));
            Assert.Equal(PossibleDuplicateSteps.ShareMinor + TransportMinor, await PossibleDuplicateSteps.MonthSpendingTotalAsync(scene.Group.Member.Client));
            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(scene.Group.Member.Client));
        }

        [Fact]
        public async Task Merge_DeletedMergedEntry_IsGoneFromTheListAndTheTotals()
        {
            Scene scene = await StartSceneAsync();
            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);

            await PersonalEntryRequests.DeleteAsync(scene.Group.Member.Client, scene.EntryId);

            Assert.Equal([scene.TransportEntryId], await PersonalEntrySteps.EntryIdsOfMonthAsync(scene.Group.Member.Client));
            Assert.Equal(TransportMinor, await PossibleDuplicateSteps.MonthSpendingTotalAsync(scene.Group.Member.Client));
        }

        [Fact]
        public async Task Merge_OnePersonsAnswer_DoesNotChangeTheOtherPersonsTotalsLimitsOrSummary()
        {
            Scene scene = await StartSceneAsync();
            Guid ownerEntry = await PossibleDuplicateSteps.AddEntryAsync(scene.Group.Owner);
            await BudgetLimitSteps.SetMonthAsync(scene.Group.Owner, 800_000);
            JsonElement ownerBefore = await PersonalEntrySteps.MonthAsync(scene.Group.Owner.Client);

            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);

            JsonElement ownerAfter = await PersonalEntrySteps.MonthAsync(scene.Group.Owner.Client);
            Assert.Equal(ownerBefore.GetRawText(), ownerAfter.GetRawText());
            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Owner.Client, ownerEntry));
        }

        private async Task<Scene> StartSceneAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid transportEntryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, TransportMinor, categoryId: PossibleDuplicateSteps.Transport);

            return new Scene(group, expenseId, entryId, transportEntryId);
        }

        private async Task<SummaryScene> StartSummarySceneAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid previousExpenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-09-20");
            Guid previousEntryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, entryDate: "2026-09-20");

            return new SummaryScene(group, expenseId, entryId, previousExpenseId, previousEntryId);
        }

        private sealed record Scene(ExpenseGroup Group, Guid ExpenseId, Guid EntryId, Guid TransportEntryId);

        private sealed record SummaryScene(ExpenseGroup Group, Guid ExpenseId, Guid EntryId, Guid PreviousExpenseId, Guid PreviousEntryId);
    }
}
