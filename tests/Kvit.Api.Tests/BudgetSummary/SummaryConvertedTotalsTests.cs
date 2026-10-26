using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.BudgetLimits;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.BudgetSummary
{
    public class SummaryConvertedTotalsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Summary_SettingNeverSet_HasNullConvertedTotalsAndKeepsTheCurrencyLines()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            Assert.True(summary.TryGetProperty("convertedTotals", out JsonElement convertedTotals));
            Assert.Equal(JsonValueKind.Null, convertedTotals.ValueKind);
            Assert.Equal(2, summary.GetProperty("currencies").GetArrayLength());
        }

        [Fact]
        public async Task Summary_MkdSetting_ConvertsEachItemWithItsOwnSavedRateAndSumsThem()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetSummarySteps.ConvertedTotalsAsync(group.Member.Client);

            BudgetSummarySteps.AssertConvertedTotals(
                convertedTotals,
                BudgetCurrencyRequests.Mkd,
                BudgetSummarySteps.ConvertedToMkdSpendingThis,
                BudgetSummarySteps.ConvertedToMkdSpendingPrevious,
                BudgetSummarySteps.ConvertedToMkdIncomeThis,
                BudgetSummarySteps.ConvertedToMkdIncomePrevious);
        }

        [Fact]
        public async Task Summary_EurSetting_ConvertsEachItemWithItsOwnSavedRateAndSumsThem()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Eur);

            JsonElement convertedTotals = await BudgetSummarySteps.ConvertedTotalsAsync(group.Member.Client);

            BudgetSummarySteps.AssertConvertedTotals(
                convertedTotals,
                BudgetCurrencyRequests.Eur,
                BudgetSummarySteps.ConvertedToEurSpendingThis,
                BudgetSummarySteps.ConvertedToEurSpendingPrevious,
                BudgetSummarySteps.ConvertedToEurIncomeThis,
                BudgetSummarySteps.ConvertedToEurIncomePrevious);
        }

        [Fact]
        public async Task Summary_ConvertedSpending_IncludesGroupSharesAlsoThoseWithoutACategory()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, null, 40_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.7000m, "2026-09-10");
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetSummarySteps.ConvertedTotalsAsync(group.Member.Client);

            BudgetSummarySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Mkd, 20_000, 30_900, 0, 0);
        }

        [Fact]
        public async Task Summary_SettingChangedFromMkdToEurAndBackToNothing_FollowsTheCurrentSetting()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);

            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);
            JsonElement inMkd = await BudgetSummarySteps.ConvertedTotalsAsync(group.Member.Client);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Eur);
            JsonElement inEur = await BudgetSummarySteps.ConvertedTotalsAsync(group.Member.Client);
            await BudgetCurrencySteps.SetAsync(group.Member, null);
            JsonElement cleared = await BudgetSummarySteps.ConvertedTotalsAsync(group.Member.Client);

            Assert.Equal(BudgetSummarySteps.ConvertedToMkdSpendingThis, inMkd.GetProperty("spendingThisMinor").GetInt64());
            Assert.Equal(BudgetSummarySteps.ConvertedToEurSpendingThis, inEur.GetProperty("spendingThisMinor").GetInt64());
            Assert.Equal(JsonValueKind.Null, cleared.ValueKind);
        }

        [Fact]
        public async Task Summary_ChangingTheSetting_ChangesOnlyConvertedTotals()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);
            JsonElement before = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);
            JsonElement afterMkd = await BudgetSummarySteps.SummaryAsync(group.Member.Client);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Eur);
            JsonElement afterEur = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            foreach (string name in new[] { "month", "previousMonth", "currencies" })
            {
                Assert.Equal(before.GetProperty(name).GetRawText(), afterMkd.GetProperty(name).GetRawText());
                Assert.Equal(before.GetProperty(name).GetRawText(), afterEur.GetProperty(name).GetRawText());
            }
        }

        [Theory]
        [InlineData("MKD")]
        [InlineData("EUR")]
        public async Task Summary_SettingOnAndNothingInEitherMonth_AnswersZerosAndNotNull(string currency)
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.SetAsync(user, currency);

            JsonElement convertedTotals = await BudgetSummarySteps.ConvertedTotalsAsync(user.Client);

            BudgetSummarySteps.AssertConvertedTotals(convertedTotals, currency, 0, 0, 0, 0);
        }

        [Fact]
        public async Task Summary_ItemsAlreadyInTheTargetCurrency_AreLeftUntouched()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.AddIncomeAsync(_app, user, 4_500_000, BudgetCurrencyRequests.Mkd, 61.2500m);
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 12_300, BudgetCurrencyRequests.Mkd, 61.2500m, "2026-09-10");
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetSummarySteps.ConvertedTotalsAsync(user.Client);

            BudgetSummarySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Mkd, 0, 12_300, 4_500_000, 0);
        }

        [Fact]
        public async Task Summary_EuroItemsWithDifferentSavedRates_AreEachConvertedOnTheirOwnAndNotAsOneSum()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_234, BudgetCurrencyRequests.Eur, 61.5000m);
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_234, BudgetCurrencyRequests.Eur, 62.0000m);
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_234, BudgetCurrencyRequests.Eur, 61.5000m, "2026-09-10");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_234, BudgetCurrencyRequests.Eur, 62.0000m, "2026-09-11");
            await BudgetCurrencySteps.UseRateAsync(_app, 70.0000m);
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetSummarySteps.ConvertedTotalsAsync(user.Client);

            BudgetSummarySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Mkd, 152_400, 152_400, 0, 0);
        }

        [Fact]
        public async Task Summary_MkdItemsWithDifferentSavedRates_AreRoundedToTheCentOneByOne()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 50_000, BudgetCurrencyRequests.Mkd, 62.0000m);
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Eur);

            JsonElement convertedTotals = await BudgetSummarySteps.ConvertedTotalsAsync(user.Client);

            BudgetSummarySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Eur, 2_445, 0, 0, 0);
        }

        [Fact]
        public async Task Summary_RateOfALaterDay_DoesNotChangeTheRateSavedWithAnEarlierItem()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            await BudgetCurrencySteps.UseRateAsync(_app, 70.0000m);
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetSummarySteps.ConvertedTotalsAsync(user.Client);

            Assert.Equal(123_100, convertedTotals.GetProperty("spendingThisMinor").GetInt64());
        }

        [Fact]
        public async Task Summary_DeletedEntriesAndExpenses_AreNotConvertedAndRestoredOnesAre()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            SignedInUser me = group.Member;
            Guid spending = await BudgetLimitSteps.AddSpendingAsync(_app, me, BudgetLimitSteps.Food, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            Guid income = await BudgetCurrencySteps.AddIncomeAsync(_app, me, 10_000, BudgetCurrencyRequests.Eur, 61.8123m, "2026-09-25");
            Guid expense = await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.7000m, "2026-09-10");
            await PersonalEntryRequests.DeleteAsync(me.Client, spending);
            await PersonalEntryRequests.DeleteAsync(me.Client, income);
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expense)).StatusCode);
            await BudgetCurrencySteps.UseRateAsync(_app, 80.0000m);
            await BudgetCurrencySteps.SetAsync(me, BudgetCurrencyRequests.Mkd);

            JsonElement whileDeleted = await BudgetSummarySteps.ConvertedTotalsAsync(me.Client);
            await PersonalEntryRequests.RestoreAsync(me.Client, spending);
            await PersonalEntryRequests.RestoreAsync(me.Client, income);
            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expense);
            JsonElement afterRestore = await BudgetSummarySteps.ConvertedTotalsAsync(me.Client);

            BudgetSummarySteps.AssertConvertedTotals(whileDeleted, BudgetCurrencyRequests.Mkd, 0, 0, 0, 0);
            BudgetSummarySteps.AssertConvertedTotals(afterRestore, BudgetCurrencyRequests.Mkd, 123_100, 30_900, 0, 618_100);
        }

        [Fact]
        public async Task Summary_GroupShareSavedWhileTheSwitchWasOff_IsNotConvertedWhileTheOthersShareIs()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);
            await BudgetCurrencySteps.SetAsync(group.Owner, BudgetCurrencyRequests.Mkd);

            JsonElement members = await BudgetSummarySteps.ConvertedTotalsAsync(group.Member.Client);
            JsonElement owners = await BudgetSummarySteps.ConvertedTotalsAsync(group.Owner.Client);

            BudgetSummarySteps.AssertConvertedTotals(members, BudgetCurrencyRequests.Mkd, 0, 0, 0, 0);
            BudgetSummarySteps.AssertConvertedTotals(owners, BudgetCurrencyRequests.Mkd, 30_900, 0, 0, 0);
        }

        [Fact]
        public async Task Summary_SharesOfADeletedGroup_AreNotConverted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);

            JsonElement convertedTotals = await BudgetSummarySteps.ConvertedTotalsAsync(group.Member.Client);

            BudgetSummarySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Mkd, 0, 0, 0, 0);
        }

        [Fact]
        public async Task Summary_AnotherPersonsSetting_DoesNotAffectMine()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Eur);

            JsonElement ownersBefore = await BudgetSummarySteps.ConvertedTotalsAsync(group.Owner.Client);
            await BudgetCurrencySteps.SetAsync(group.Owner, BudgetCurrencyRequests.Mkd);
            JsonElement members = await BudgetSummarySteps.ConvertedTotalsAsync(group.Member.Client);
            JsonElement owners = await BudgetSummarySteps.ConvertedTotalsAsync(group.Owner.Client);

            Assert.Equal(JsonValueKind.Null, ownersBefore.ValueKind);
            BudgetSummarySteps.AssertConvertedTotals(members, BudgetCurrencyRequests.Eur, 820, 0, 0, 0);
            BudgetSummarySteps.AssertConvertedTotals(owners, BudgetCurrencyRequests.Mkd, 50_000, 0, 0, 0);
        }

        [Fact]
        public async Task Summary_AnotherPersonsEntries_AreNeverConverted()
        {
            SignedInUser me = await _app.SignUpAsync();
            SignedInUser stranger = await _app.SignUpAsync("Bojan");
            await BudgetLimitSteps.AddSpendingAsync(_app, stranger, BudgetLimitSteps.Food, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            await BudgetCurrencySteps.AddIncomeAsync(_app, stranger, 10_000, BudgetCurrencyRequests.Eur, 61.8123m, "2026-09-25");
            await BudgetCurrencySteps.SetAsync(me, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetSummarySteps.ConvertedTotalsAsync(me.Client);

            BudgetSummarySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Mkd, 0, 0, 0, 0);
        }

        [Theory]
        [InlineData("MKD")]
        [InlineData("EUR")]
        public async Task Summary_ConvertedTotals_AgreeWithTheMonthAnswersOfBothMonths(string currency)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);
            await BudgetCurrencySteps.SetAsync(group.Member, currency);
            HttpClient client = group.Member.Client;

            JsonElement convertedTotals = await BudgetSummarySteps.ConvertedTotalsAsync(client);
            JsonElement thisMonth = await BudgetCurrencySteps.ConvertedTotalsAsync(client, BudgetSummarySteps.ThisMonth);
            JsonElement previousMonth = await BudgetCurrencySteps.ConvertedTotalsAsync(client, BudgetSummarySteps.PreviousMonth);

            BudgetSummarySteps.AssertConvertedTotals(
                convertedTotals,
                currency,
                SpendingAndSharesOf(thisMonth),
                SpendingAndSharesOf(previousMonth),
                thisMonth.GetProperty("incomeMinor").GetInt64(),
                previousMonth.GetProperty("incomeMinor").GetInt64());
        }

        [Fact]
        public async Task Summary_ConvertedTotalsOfEachMonthOnTheEdgesOfAYear_UseOnlyThatMonthsItems()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.0000m, "2025-11-30");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 2_000, BudgetCurrencyRequests.Eur, 62.0000m, "2025-12-31");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 4_000, BudgetCurrencyRequests.Eur, 63.0000m, "2026-01-01");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 8_000, BudgetCurrencyRequests.Eur, 64.0000m, "2026-02-01");
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetSummarySteps.ConvertedTotalsAsync(user.Client, "2026-01");

            BudgetSummarySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Mkd, 252_000, 124_000, 0, 0);
        }

        private static long SpendingAndSharesOf(JsonElement convertedTotalsOfAMonth)
        {
            return convertedTotalsOfAMonth.GetProperty("spendingMinor").GetInt64() + convertedTotalsOfAMonth.GetProperty("groupSharesMinor").GetInt64();
        }
    }
}
