using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.BudgetLimits;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.BudgetSummary
{
    public class SummaryGroupSharesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Summary_GroupShareOfThisMonth_CountsOnlyTheCallersShareInItsCategoryAndCurrency()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 50_000, 0, 0, 0);
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 50_000, 0));
        }

        [Fact]
        public async Task Summary_GroupShareOfThePreviousMonth_CountsAsPrevious()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Transport, 2_000, BudgetCurrencyRequests.Eur, 61.0000m, "2026-09-30");

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Eur, 0, 1_000, 0, 0);
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Eur, (BudgetLimitSteps.Transport, 0, 1_000));
        }

        [Fact]
        public async Task Summary_GroupShareWithPersonalSpendingInTheSameCategory_AreAddedIntoOneCategoryRow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, group.Member, BudgetLimitSteps.Food, 30_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 80_000, 0));
        }

        [Fact]
        public async Task Summary_GroupExpenseWithoutACategory_CountsInTheCurrencyTotalAndInNoCategoryRow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, null, 40_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 70_000, 0, 0, 0);
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 50_000, 0));
        }

        [Fact]
        public async Task Summary_OnlyGroupExpensesWithoutACategory_ListTheCurrencyWithAnEmptyCategoriesList()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, null, 40_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-10");

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 0, 20_000, 0, 0);
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd);
        }

        [Fact]
        public async Task Summary_ShareSavedWhileTheSwitchWasOff_IsNotCountedForThatPersonButIsForTheOther()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);

            JsonElement members = await BudgetSummarySteps.SummaryAsync(group.Member.Client);
            JsonElement owners = await BudgetSummarySteps.SummaryAsync(group.Owner.Client);

            BudgetSummarySteps.AssertNothingToCompare(members);
            BudgetSummarySteps.AssertCurrencyRow(owners, BudgetCurrencyRequests.Mkd, 50_000, 0, 0, 0);
        }

        [Fact]
        public async Task Summary_ShareSavedBeforeTheSwitchWentOff_IsStillCounted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 50_000, 0, 0, 0);
        }

        [Fact]
        public async Task Summary_SharesOfADeletedGroup_AreNotCounted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-10");
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertNothingToCompare(summary);
        }

        [Theory]
        [InlineData("Closing")]
        [InlineData("Finished")]
        public async Task Summary_SharesOfAClosingOrFinishedGroup_AreStillCounted(string status)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-10");
            await GroupRows.SetStatusAsync(_app, group.GroupId, status);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 50_000, 50_000, 0, 0);
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 50_000, 50_000));
        }

        [Fact]
        public async Task Summary_DeletedGroupExpense_IsLeftOutAndRestoredExpenseIsBack()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId)).StatusCode);

            JsonElement whileDeleted = await BudgetSummarySteps.SummaryAsync(group.Member.Client);
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId)).StatusCode);
            JsonElement afterRestore = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertNothingToCompare(whileDeleted);
            BudgetSummarySteps.AssertCurrencyRow(afterRestore, BudgetCurrencyRequests.Mkd, 50_000, 0, 0, 0);
        }

        [Fact]
        public async Task Summary_AnotherPersonsShare_IsNeverCounted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 70_000), new ShareInput(group.MemberRowId, 30_000)];
            ExpenseInput input = ExpenseInputs.Of("Exact", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, shares) with { CategoryId = BudgetLimitSteps.Food };
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");

            JsonElement ownerSummary = await BudgetSummarySteps.SummaryAsync(group.Owner.Client);
            JsonElement memberSummary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);
            JsonElement strangerSummary = await BudgetSummarySteps.SummaryAsync(stranger.Client);

            BudgetSummarySteps.AssertCurrencyRow(ownerSummary, BudgetCurrencyRequests.Mkd, 70_000, 0, 0, 0);
            BudgetSummarySteps.AssertCurrencyRow(memberSummary, BudgetCurrencyRequests.Mkd, 30_000, 0, 0, 0);
            BudgetSummarySteps.AssertNothingToCompare(strangerSummary);
        }

        [Fact]
        public async Task Summary_PayerWhoOwesNothing_HasNoLineWhileTheOtherPersonHasOne()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 0), new ShareInput(group.MemberRowId, ExpenseInputs.OneThousandMkd)];
            ExpenseInput input = ExpenseInputs.Of("Exact", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, shares) with { CategoryId = BudgetLimitSteps.Food };
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            JsonElement ownerSummary = await BudgetSummarySteps.SummaryAsync(group.Owner.Client);
            JsonElement memberSummary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertNothingToCompare(ownerSummary);
            BudgetSummarySteps.AssertCurrencyRow(memberSummary, BudgetCurrencyRequests.Mkd, ExpenseInputs.OneThousandMkd, 0, 0, 0);
        }

        [Fact]
        public async Task Summary_SettlementsAndPaybacks_AreNeverSpendingOrIncome()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId);

            JsonElement ownerSummary = await BudgetSummarySteps.SummaryAsync(group.Owner.Client);
            JsonElement memberSummary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertNothingToCompare(ownerSummary);
            BudgetSummarySteps.AssertNothingToCompare(memberSummary);
        }

        [Fact]
        public async Task Summary_GroupShares_NeverBecomeIncome()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId);

            JsonElement ownerSummary = await BudgetSummarySteps.SummaryAsync(group.Owner.Client);
            JsonElement memberSummary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCurrencyRow(ownerSummary, BudgetCurrencyRequests.Mkd, 50_000, 0, 0, 0);
            BudgetSummarySteps.AssertCurrencyRow(memberSummary, BudgetCurrencyRequests.Mkd, 50_000, 0, 0, 0);
        }

        [Fact]
        public async Task Summary_SharesOfAPlainNameClaimedLater_CountForTheClaimerInBothMonths()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid markoRowId = group.PlainRowIds[0];
            ExpenseInput thisMonth = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, markoRowId) with { CategoryId = BudgetLimitSteps.Food };
            ExpenseInput previousMonth = thisMonth with { ExpenseDate = "2026-09-10" };
            await _app.AddExpenseAsync(group.Owner, group.GroupId, thisMonth);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, previousMonth);
            Assert.Equal(HttpStatusCode.NoContent, (await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, markoRowId)).StatusCode);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, BudgetShareSteps.HalfOfOneThousandMkd, BudgetShareSteps.HalfOfOneThousandMkd, 0, 0);
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        public async Task Summary_MemberWhoLeftOrWasRemoved_KeepsThePastShares(string endKind)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 50_000, 0, 0, 0);
        }

        [Fact]
        public async Task Summary_OneBillCreator_SeesTheirShareOfTheBill()
        {
            SignedInUser caller = await _app.SignUpAsync();
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko") with { CategoryId = food, Title = "Pizza" };
            await _app.CreateOneBillAsync(caller, input);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(caller.Client);

            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, BudgetShareSteps.HalfOfOneThousandMkd, 0, 0, 0);
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (food, BudgetShareSteps.HalfOfOneThousandMkd, 0));
        }

        [Fact]
        public async Task Summary_MkdAndEurShares_StayOnTheirOwnLinesNeverConverted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.0000m);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 50_000, 0, 0, 0);
            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Eur, 500, 0, 0, 0);
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 50_000, 0));
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Eur, (BudgetLimitSteps.Food, 500, 0));
        }

        [Fact]
        public async Task Summary_GroupExpenseBoundaryDays_BelongToTheirOwnMonthOnly()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 2_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-11-30");
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 4_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-12-01");
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 8_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-12-31");
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 16_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-01-01");
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 32_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-01-31");
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 64_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-02-01");

            JsonElement january = await BudgetSummarySteps.SummaryAsync(group.Member.Client, "2026-01");

            BudgetSummarySteps.AssertCurrencyRow(january, BudgetCurrencyRequests.Mkd, 24_000, 6_000, 0, 0);
            BudgetSummarySteps.AssertCategories(january, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 24_000, 6_000));
        }
    }
}
