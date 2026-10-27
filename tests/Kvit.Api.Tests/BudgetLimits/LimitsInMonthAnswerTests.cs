using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalCategories;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Api.Tests.RecurringIncomes;
using Xunit;

namespace Kvit.Api.Tests.BudgetLimits
{
    public class LimitsInMonthAnswerTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const long FoodSpent = 379_900;
        private const long FoodIncludedEur = 3_734;
        private const long TransportSpent = 187_100;
        private const long TransportIncludedEur = 2_550;
        private const long MonthSpent = FoodSpent + TransportSpent;
        private const long MonthIncludedEur = FoodIncludedEur + TransportIncludedEur;

        [Fact]
        public async Task Month_NoLimitsSet_HasAnEmptyLimitsObjectAndNotAMissingOne()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddMixedMonthAsync(_app, group);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(group.Member.Client);

            BudgetLimitSteps.AssertNoLimits(limits);
        }

        [Fact]
        public async Task Month_MonthLimitOnly_HasTheMonthRowWithANullCategoryAndNoCategoryRows()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(user.Client);

            Assert.Equal(BudgetLimitSteps.LimitsFieldNames, GroupRequests.PropertyNamesOf(limits));
            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, 4_500_000, 0, 0, 0);
            Assert.Equal(0, limits.GetProperty("categories").GetArrayLength());
        }

        [Fact]
        public async Task Month_CategoryLimitOnly_HasANullMonthAndOneCategoryRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(user.Client);

            Assert.Equal(JsonValueKind.Null, limits.GetProperty("month").ValueKind);
            JsonElement row = Assert.Single(limits.GetProperty("categories").EnumerateArray());
            BudgetLimitSteps.AssertRow(row, BudgetLimitSteps.Food, 800_000, 0, 0, 0);
        }

        [Fact]
        public async Task Month_CategorySpentIsPersonalSpendingPlusGroupSharesEachEuroItemConvertedWithItsOwnRate()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddMixedMonthAsync(_app, group);
            await BudgetLimitSteps.SetCategoryAsync(group.Member, BudgetLimitSteps.Food, 1_000_000);
            await BudgetLimitSteps.SetCategoryAsync(group.Member, BudgetLimitSteps.Transport, 150_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(group.Member.Client);

            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(limits, BudgetLimitSteps.Food), BudgetLimitSteps.Food, 1_000_000, FoodSpent, 0, FoodIncludedEur);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(limits, BudgetLimitSteps.Transport), BudgetLimitSteps.Transport, 150_000, TransportSpent, 37_100, TransportIncludedEur);
        }

        [Fact]
        public async Task Month_MonthRowCountsEverySpendingAndGroupShareButNeverIncome()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddMixedMonthAsync(_app, group);
            await BudgetLimitSteps.SetMonthAsync(group.Member, 800_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(group.Member.Client);

            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, 800_000, MonthSpent, 0, MonthIncludedEur);
        }

        [Fact]
        public async Task Month_MonthRowAboveTheLimit_ShowsTheOverAmountInDeni()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddMixedMonthAsync(_app, group);
            await BudgetLimitSteps.SetMonthAsync(group.Member, 500_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(group.Member.Client);

            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, 500_000, MonthSpent, 67_000, MonthIncludedEur);
        }

        [Theory]
        [InlineData(567_100, 0)]
        [InlineData(567_000, 0)]
        [InlineData(566_900, 100)]
        [InlineData(100, 566_900)]
        public async Task Month_SpentAgainstTheLimit_OverIsZeroUpToAndIncludingTheLimitAndGrowsByTheDeniAbove(long limitMinor, long expectedOver)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddMixedMonthAsync(_app, group);
            await BudgetLimitSteps.SetMonthAsync(group.Member, limitMinor);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(group.Member.Client);

            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, limitMinor, MonthSpent, expectedOver, MonthIncludedEur);
        }

        [Fact]
        public async Task Month_LimitWithNoSpending_ShowsZeroSpentZeroOverAndZeroEuro()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Transport, 30_000, BudgetCurrencyRequests.Mkd, 61.0000m);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(user.Client);

            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(limits, BudgetLimitSteps.Food), BudgetLimitSteps.Food, 800_000, 0, 0, 0);
        }

        [Fact]
        public async Task Month_OnlyMkdItems_HaveZeroIncludedEuro()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);
            await BudgetLimitSteps.SetMonthAsync(user, 800_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(user.Client);

            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, 800_000, 100_000, 0, 0);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(limits, BudgetLimitSteps.Food), BudgetLimitSteps.Food, 800_000, 100_000, 0, 0);
        }

        [Fact]
        public async Task Month_EuroItemsWithDifferentSavedRates_AreEachConvertedOnTheirOwnAndNotAsOneSum()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_234, BudgetCurrencyRequests.Eur, 61.5000m);
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_234, BudgetCurrencyRequests.Eur, 62.0000m);
            await BudgetCurrencySteps.UseRateAsync(_app, 70.0000m);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(user.Client);

            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(limits, BudgetLimitSteps.Food), BudgetLimitSteps.Food, 800_000, 152_400, 0, 2_468);
        }

        [Fact]
        public async Task Month_GroupShareWithoutACategory_CountsInTheMonthRowAndInNoCategoryRow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, null, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await BudgetLimitSteps.SetMonthAsync(group.Member, 800_000);
            await BudgetLimitSteps.SetCategoryAsync(group.Member, BudgetLimitSteps.Food, 800_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(group.Member.Client);

            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, 800_000, 80_900, 0, 500);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(limits, BudgetLimitSteps.Food), BudgetLimitSteps.Food, 800_000, 30_900, 0, 500);
        }

        [Fact]
        public async Task Month_CustomCategory_CountsItsOwnSpendingAndFeedsTheMonthRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, pets, 20_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSpendingAsync(_app, user, pets, 1_000, BudgetCurrencyRequests.Eur, 61.5000m);
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 10_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.SetCategoryAsync(user, pets, 100_000);
            await BudgetLimitSteps.SetMonthAsync(user, 100_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(user.Client);

            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(limits, pets), pets, 100_000, 81_500, 0, 1_000);
            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, 100_000, 91_500, 0, 1_000);
        }

        [Fact]
        public async Task Month_ArchivedCategoryWithALimit_StillShowsItsSpending()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, pets, 20_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.SetCategoryAsync(user, pets, 10_000);
            await PersonalCategoryRequests.ArchiveAsync(user.Client, pets);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(user.Client);

            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(limits, pets), pets, 10_000, 20_000, 10_000, 0);
        }

        [Fact]
        public async Task Month_CategoryRows_AreBuiltInsFirstInTheirOrderThenCustomWhateverOrderTheLimitsWereSet()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            await BudgetLimitSteps.SetCategoryAsync(user, pets, 100_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Transport, 200_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 300_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(user.Client);

            Guid[] order = [.. limits.GetProperty("categories").EnumerateArray().Select(row => row.GetProperty("categoryId").GetGuid())];
            Assert.Equal([BudgetLimitSteps.Food, BudgetLimitSteps.Transport, pets], order);
        }

        [Fact]
        public async Task Month_LimitSetThisMonth_AppliesFromThisMonthOnWithEachMonthsOwnSpendingAndNotToEarlierMonths()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            SignedInUser me = group.Member;
            await BudgetLimitSteps.AddSpendingAsync(_app, me, BudgetLimitSteps.Food, 10_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-30");
            await BudgetLimitSteps.AddSpendingAsync(_app, me, BudgetLimitSteps.Food, 20_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-10-01");
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.0000m, "2026-10-31");
            CatchUpSteps.SetNow(_app, "2026-11-20T12:00:00Z");
            await BudgetLimitSteps.AddSpendingAsync(_app, me, BudgetLimitSteps.Food, 40_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-11-01");
            CatchUpSteps.SetNow(_app, "2026-10-12T12:00:00Z");
            await BudgetLimitSteps.SetMonthAsync(me, 100_000);
            await BudgetLimitSteps.SetCategoryAsync(me, BudgetLimitSteps.Food, 100_000);
            _app.Clock.SetTo(ExpensesApp.ClockStart);

            JsonElement september = await BudgetLimitSteps.LimitsAsync(me.Client, "2026-09");
            JsonElement october = await BudgetLimitSteps.LimitsAsync(me.Client, "2026-10");
            JsonElement november = await BudgetLimitSteps.LimitsAsync(me.Client, "2026-11");
            JsonElement farFuture = await BudgetLimitSteps.LimitsAsync(me.Client, "2027-08");
            JsonElement distantPast = await BudgetLimitSteps.LimitsAsync(me.Client, "2025-01");

            BudgetLimitSteps.AssertNoLimits(september);
            BudgetLimitSteps.AssertRow(october.GetProperty("month"), null, 100_000, 50_500, 0, 500);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(october, BudgetLimitSteps.Food), BudgetLimitSteps.Food, 100_000, 50_500, 0, 500);
            BudgetLimitSteps.AssertRow(november.GetProperty("month"), null, 100_000, 40_000, 0, 0);
            BudgetLimitSteps.AssertRow(farFuture.GetProperty("month"), null, 100_000, 0, 0, 0);
            BudgetLimitSteps.AssertNoLimits(distantPast);
        }

        [Fact]
        public async Task Month_RemovedLimit_IsGoneFromTheAnswerAndTheOtherStays()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);

            await BudgetLimitRequests.DeleteCategoryAsync(user.Client, BudgetLimitSteps.Food);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(user.Client);
            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, 4_500_000, 0, 0, 0);
            Assert.Equal(0, limits.GetProperty("categories").GetArrayLength());
        }

        [Fact]
        public async Task Month_DeletedAndRestoredItems_LeaveAndReturnToTheSpentAmount()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            SignedInUser me = group.Member;
            Guid entryId = await BudgetLimitSteps.AddSpendingAsync(_app, me, BudgetLimitSteps.Food, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            Guid expenseId = await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await BudgetLimitSteps.SetMonthAsync(me, 800_000);
            await PersonalEntryRequests.DeleteAsync(me.Client, entryId);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            await BudgetCurrencySteps.UseRateAsync(_app, 80.0000m);

            JsonElement whileDeleted = await BudgetLimitSteps.LimitsAsync(me.Client);
            await PersonalEntryRequests.RestoreAsync(me.Client, entryId);
            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);
            JsonElement afterRestore = await BudgetLimitSteps.LimitsAsync(me.Client);

            BudgetLimitSteps.AssertRow(whileDeleted.GetProperty("month"), null, 800_000, 0, 0, 0);
            BudgetLimitSteps.AssertRow(afterRestore.GetProperty("month"), null, 800_000, 154_000, 0, 2_500);
        }

        [Fact]
        public async Task Month_GroupShareSavedWhileTheSwitchWasOff_IsNotCountedWhileTheOthersShareIs()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await BudgetLimitSteps.SetMonthAsync(group.Member, 800_000);
            await BudgetLimitSteps.SetMonthAsync(group.Owner, 800_000);

            JsonElement members = await BudgetLimitSteps.LimitsAsync(group.Member.Client);
            JsonElement owners = await BudgetLimitSteps.LimitsAsync(group.Owner.Client);

            BudgetLimitSteps.AssertRow(members.GetProperty("month"), null, 800_000, 0, 0, 0);
            BudgetLimitSteps.AssertRow(owners.GetProperty("month"), null, 800_000, 30_900, 0, 500);
        }

        [Fact]
        public async Task Month_GroupShareSavedBeforeTheSwitchWentOff_IsStillCounted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            await BudgetLimitSteps.SetMonthAsync(group.Member, 800_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(group.Member.Client);

            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, 800_000, 30_900, 0, 500);
        }

        [Fact]
        public async Task Month_SharesOfADeletedGroup_AreNotCounted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await BudgetLimitSteps.SetMonthAsync(group.Member, 800_000);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(group.Member.Client);

            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, 800_000, 0, 0, 0);
        }

        [Theory]
        [InlineData("Closing")]
        [InlineData("Finished")]
        public async Task Month_SharesOfAClosingOrFinishedGroup_AreStillCounted(string status)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await BudgetLimitSteps.SetMonthAsync(group.Member, 800_000);
            await GroupRows.SetStatusAsync(_app, group.GroupId, status);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(group.Member.Client);

            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, 800_000, 30_900, 0, 500);
        }

        [Fact]
        public async Task Month_AnotherPersonsSpendingAndShares_AreNeverCounted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            await BudgetLimitSteps.AddSpendingAsync(_app, stranger, BudgetLimitSteps.Food, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            await BudgetLimitSteps.AddSharedExpenseAsync(_app, group, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await BudgetLimitSteps.SetMonthAsync(stranger, 800_000);
            await BudgetLimitSteps.SetCategoryAsync(stranger, BudgetLimitSteps.Food, 800_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(stranger.Client);

            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, 800_000, 123_100, 0, 2_000);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(limits, BudgetLimitSteps.Food), BudgetLimitSteps.Food, 800_000, 123_100, 0, 2_000);
        }

        [Fact]
        public async Task Month_LimitsOfOnePerson_NeverShowForAnother()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync("Bojan");
            await BudgetLimitSteps.SetMonthAsync(first, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(first, BudgetLimitSteps.Food, 800_000);

            JsonElement seenBySecond = await BudgetLimitSteps.LimitsAsync(second.Client);
            await BudgetLimitSteps.SetMonthAsync(second, 7_000_000);
            JsonElement seenByFirst = await BudgetLimitSteps.LimitsAsync(first.Client);

            BudgetLimitSteps.AssertNoLimits(seenBySecond);
            BudgetLimitSteps.AssertRow(seenByFirst.GetProperty("month"), null, 4_500_000, 0, 0, 0);
            Assert.Equal(1, seenByFirst.GetProperty("categories").GetArrayLength());
        }

        [Theory]
        [InlineData("MKD")]
        [InlineData("EUR")]
        public async Task Month_BudgetTotalCurrencySetting_NeverChangesTheLimits(string currency)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddMixedMonthAsync(_app, group);
            await BudgetLimitSteps.SetMonthAsync(group.Member, 500_000);
            await BudgetLimitSteps.SetCategoryAsync(group.Member, BudgetLimitSteps.Food, 1_000_000);
            JsonElement before = await BudgetLimitSteps.LimitsAsync(group.Member.Client);

            await BudgetCurrencySteps.SetAsync(group.Member, currency);
            JsonElement after = await BudgetLimitSteps.LimitsAsync(group.Member.Client);

            Assert.Equal(before.GetRawText(), after.GetRawText());
            BudgetLimitSteps.AssertRow(after.GetProperty("month"), null, 500_000, MonthSpent, 67_000, MonthIncludedEur);
        }

        [Fact]
        public async Task Month_SettingLimits_ChangesNoOtherFieldOfTheAnswer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetLimitSteps.AddMixedMonthAsync(_app, group);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);
            JsonElement before = await PersonalEntrySteps.MonthAsync(group.Member.Client);

            await BudgetLimitSteps.SetMonthAsync(group.Member, 500_000);
            await BudgetLimitSteps.SetCategoryAsync(group.Member, BudgetLimitSteps.Food, 1_000_000);
            JsonElement after = await PersonalEntrySteps.MonthAsync(group.Member.Client);

            foreach (string name in new[] { "month", "entries", "totals", "incomeTotals", "groupShares", "convertedTotals" })
            {
                Assert.Equal(before.GetProperty(name).GetRawText(), after.GetProperty(name).GetRawText());
            }
        }

        [Fact]
        public async Task Month_RateOfALaterDay_DoesNotChangeTheRateSavedWithAnEarlierItem()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            await BudgetCurrencySteps.UseRateAsync(_app, 70.0000m);
            await BudgetLimitSteps.SetMonthAsync(user, 800_000);

            JsonElement limits = await BudgetLimitSteps.LimitsAsync(user.Client);

            BudgetLimitSteps.AssertRow(limits.GetProperty("month"), null, 800_000, 123_100, 0, 2_000);
        }

        [Fact]
        public async Task Month_ByAGuest_StillAnswers403GuestNotAllowed()
        {
            (ExpenseGroup _, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");

            HttpResponseMessage response = await PersonalEntryRequests.ListAsync(guest.Client);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
