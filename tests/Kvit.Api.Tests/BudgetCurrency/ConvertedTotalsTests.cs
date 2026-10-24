using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.BudgetCurrency
{
    public class ConvertedTotalsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Month_SettingNeverSet_HasNullConvertedTotalsAndKeepsTheOtherAnswers()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetCurrencySteps.AddMixedMonthAsync(_app, group);

            JsonElement body = await PersonalEntrySteps.MonthAsync(group.Member.Client);

            Assert.True(body.TryGetProperty("convertedTotals", out JsonElement convertedTotals));
            Assert.Equal(JsonValueKind.Null, convertedTotals.ValueKind);
            Assert.Equal(5, body.GetProperty("entries").EnumerateArray().Count(entry => entry.GetProperty("kind").GetString() == PersonalEntryRequests.SpendingKind));
            Assert.Equal(2, body.GetProperty("totals").GetArrayLength());
            Assert.Equal(2, body.GetProperty("incomeTotals").GetArrayLength());
            Assert.Equal(3, body.GetProperty("groupShares").GetArrayLength());
        }

        [Fact]
        public async Task Month_MkdSetting_ConvertsEachItemWithItsOwnSavedRateAndSumsThem()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetCurrencySteps.AddMixedMonthAsync(_app, group);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetCurrencySteps.ConvertedTotalsAsync(group.Member.Client);

            BudgetCurrencySteps.AssertConvertedTotals(
                convertedTotals,
                currency: BudgetCurrencyRequests.Mkd,
                spendingMinor: 425_500,
                incomeMinor: 6_618_100,
                groupSharesMinor: 112_100,
                leftMinor: 6_080_500);
        }

        [Fact]
        public async Task Month_EurSetting_ConvertsEachItemWithItsOwnSavedRateAndSumsThem()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetCurrencySteps.AddMixedMonthAsync(_app, group);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Eur);

            JsonElement convertedTotals = await BudgetCurrencySteps.ConvertedTotalsAsync(group.Member.Client);

            BudgetCurrencySteps.AssertConvertedTotals(
                convertedTotals,
                currency: BudgetCurrencyRequests.Eur,
                spendingMinor: 6_913,
                incomeMinor: 107_469,
                groupSharesMinor: 1_814,
                leftMinor: 98_742);
        }

        [Fact]
        public async Task Month_SettingChangedFromMkdToEurAndBackToNothing_FollowsTheCurrentSetting()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetCurrencySteps.AddMixedMonthAsync(_app, group);

            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);
            JsonElement inMkd = await BudgetCurrencySteps.ConvertedTotalsAsync(group.Member.Client);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Eur);
            JsonElement inEur = await BudgetCurrencySteps.ConvertedTotalsAsync(group.Member.Client);
            await BudgetCurrencySteps.SetAsync(group.Member, null);
            JsonElement cleared = await BudgetCurrencySteps.ConvertedTotalsAsync(group.Member.Client);

            Assert.Equal(6_080_500, inMkd.GetProperty("leftMinor").GetInt64());
            Assert.Equal(98_742, inEur.GetProperty("leftMinor").GetInt64());
            Assert.Equal(JsonValueKind.Null, cleared.ValueKind);
        }

        [Fact]
        public async Task Month_ChangingTheSetting_ChangesOnlyConvertedTotals()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetCurrencySteps.AddMixedMonthAsync(_app, group);
            JsonElement before = await PersonalEntrySteps.MonthAsync(group.Member.Client);

            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);
            JsonElement afterMkd = await PersonalEntrySteps.MonthAsync(group.Member.Client);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Eur);
            JsonElement afterEur = await PersonalEntrySteps.MonthAsync(group.Member.Client);

            foreach (string name in new[] { "month", "entries", "totals", "incomeTotals", "groupShares" })
            {
                Assert.Equal(before.GetProperty(name).GetRawText(), afterMkd.GetProperty(name).GetRawText());
                Assert.Equal(before.GetProperty(name).GetRawText(), afterEur.GetProperty(name).GetRawText());
            }
        }

        [Fact]
        public async Task Month_ConvertedTotals_LeaveTheGroupShareRowsAtEightFieldsAndInTheirOwnCurrency()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetCurrencySteps.AddMixedMonthAsync(_app, group);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);

            JsonElement shares = await BudgetShareSteps.GroupSharesAsync(group.Member.Client);

            Assert.Equal(3, shares.GetArrayLength());
            foreach (JsonElement row in shares.EnumerateArray())
            {
                Assert.Equal(BudgetShareSteps.GroupShareFieldNames, GroupRequests.PropertyNamesOf(row));
            }

            long[] eurShares = [.. shares.EnumerateArray()
                .Where(row => row.GetProperty("currency").GetString() == BudgetCurrencyRequests.Eur)
                .Select(row => row.GetProperty("shareMinor").GetInt64())];
            Assert.Equal([500L, 500L], eurShares);
        }

        [Theory]
        [InlineData("MKD")]
        [InlineData("EUR")]
        public async Task Month_SettingOnAndNothingInTheMonth_AnswersZerosAndNotNull(string currency)
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.SetAsync(user, currency);

            JsonElement convertedTotals = await BudgetCurrencySteps.ConvertedTotalsAsync(user.Client);

            BudgetCurrencySteps.AssertConvertedTotals(convertedTotals, currency, 0, 0, 0, 0);
        }

        [Fact]
        public async Task Month_OnlyIncomeInTheTargetCurrency_IsLeftUnchangedAndLeftEqualsIt()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.AddIncomeAsync(_app, user, 4_500_000, BudgetCurrencyRequests.Mkd, 61.2500m);
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetCurrencySteps.ConvertedTotalsAsync(user.Client);

            BudgetCurrencySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Mkd, 0, 4_500_000, 0, 4_500_000);
        }

        [Fact]
        public async Task Month_SpendingWithoutIncome_HasANegativeLeft()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.AddSpendingAsync(_app, user, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetCurrencySteps.ConvertedTotalsAsync(user.Client);

            BudgetCurrencySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Mkd, 123_100, 0, 0, -123_100);
        }

        [Fact]
        public async Task Month_RateOfALaterDay_DoesNotChangeTheRateSavedWithAnEarlierItem()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.AddSpendingAsync(_app, user, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            await BudgetCurrencySteps.UseRateAsync(_app, 70.0000m);
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetCurrencySteps.ConvertedTotalsAsync(user.Client);

            Assert.Equal(123_100, convertedTotals.GetProperty("spendingMinor").GetInt64());
        }

        [Fact]
        public async Task Month_DeletedEntriesAndDeletedExpenses_AreNotConverted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            SignedInUser me = group.Member;
            await BudgetCurrencySteps.AddSpendingAsync(_app, me, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            await BudgetCurrencySteps.AddIncomeAsync(_app, me, 10_000, BudgetCurrencyRequests.Eur, 61.8123m);
            await BudgetCurrencySteps.AddSharedExpenseAsync(_app, group, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            Guid deletedSpending = await BudgetCurrencySteps.AddSpendingAsync(_app, me, 9_999, BudgetCurrencyRequests.Eur, 70.0000m);
            Guid deletedIncome = await BudgetCurrencySteps.AddIncomeAsync(_app, me, 8_888, BudgetCurrencyRequests.Eur, 71.0000m);
            Guid deletedExpense = await BudgetCurrencySteps.AddSharedExpenseAsync(_app, group, 7_000, BudgetCurrencyRequests.Eur, 72.0000m);
            Assert.Equal(HttpStatusCode.NoContent, (await PersonalEntryRequests.DeleteAsync(me.Client, deletedSpending)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await PersonalEntryRequests.DeleteAsync(me.Client, deletedIncome)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, deletedExpense)).StatusCode);
            await BudgetCurrencySteps.SetAsync(me, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetCurrencySteps.ConvertedTotalsAsync(me.Client);

            BudgetCurrencySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Mkd, 123_100, 618_100, 30_900, 464_100);
        }

        [Fact]
        public async Task Month_RestoredEntryAndRestoredExpense_AreConvertedAgainWithTheirSavedRates()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            SignedInUser me = group.Member;
            Guid spending = await BudgetCurrencySteps.AddSpendingAsync(_app, me, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            Guid expense = await BudgetCurrencySteps.AddSharedExpenseAsync(_app, group, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await PersonalEntryRequests.DeleteAsync(me.Client, spending);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expense);
            await BudgetCurrencySteps.UseRateAsync(_app, 80.0000m);
            await BudgetCurrencySteps.SetAsync(me, BudgetCurrencyRequests.Mkd);
            BudgetCurrencySteps.AssertConvertedTotals(await BudgetCurrencySteps.ConvertedTotalsAsync(me.Client), BudgetCurrencyRequests.Mkd, 0, 0, 0, 0);

            await PersonalEntryRequests.RestoreAsync(me.Client, spending);
            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expense);

            BudgetCurrencySteps.AssertConvertedTotals(await BudgetCurrencySteps.ConvertedTotalsAsync(me.Client), BudgetCurrencyRequests.Mkd, 123_100, 0, 30_900, -154_000);
        }

        [Fact]
        public async Task Month_GroupShareSavedWhileTheSwitchWasOff_IsNotConvertedWhileTheOthersShareIs()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            await BudgetCurrencySteps.AddSharedExpenseAsync(_app, group, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);
            await BudgetCurrencySteps.SetAsync(group.Owner, BudgetCurrencyRequests.Mkd);

            JsonElement members = await BudgetCurrencySteps.ConvertedTotalsAsync(group.Member.Client);
            JsonElement owners = await BudgetCurrencySteps.ConvertedTotalsAsync(group.Owner.Client);

            BudgetCurrencySteps.AssertConvertedTotals(members, BudgetCurrencyRequests.Mkd, 0, 0, 0, 0);
            BudgetCurrencySteps.AssertConvertedTotals(owners, BudgetCurrencyRequests.Mkd, 0, 0, 30_900, -30_900);
        }

        [Fact]
        public async Task Month_GroupShareSavedBeforeTheSwitchWentOff_IsStillConverted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetCurrencySteps.AddSharedExpenseAsync(_app, group, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetCurrencySteps.ConvertedTotalsAsync(group.Member.Client);

            BudgetCurrencySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Mkd, 0, 0, 30_900, -30_900);
        }

        [Fact]
        public async Task Month_SharesOfADeletedGroup_AreNotConverted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetCurrencySteps.AddSharedExpenseAsync(_app, group, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Mkd);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);

            JsonElement convertedTotals = await BudgetCurrencySteps.ConvertedTotalsAsync(group.Member.Client);

            BudgetCurrencySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Mkd, 0, 0, 0, 0);
        }

        [Fact]
        public async Task Month_FirstAndLastDayOfAMonth_BelongToThatMonthsConvertedTotalsOnly()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            SignedInUser me = group.Member;
            await BudgetCurrencySteps.AddSpendingAsync(_app, me, 1_000, BudgetCurrencyRequests.Eur, 61.0000m, "2026-09-30");
            await BudgetCurrencySteps.AddSpendingAsync(_app, me, 1_000, BudgetCurrencyRequests.Eur, 62.0000m, "2026-10-01");
            await BudgetCurrencySteps.AddSharedExpenseAsync(_app, group, 2_000, BudgetCurrencyRequests.Eur, 61.0000m, "2026-09-30");
            await BudgetCurrencySteps.AddSharedExpenseAsync(_app, group, 2_000, BudgetCurrencyRequests.Eur, 62.0000m, "2026-10-01");
            await BudgetCurrencySteps.AddSharedExpenseAsync(_app, group, 2_000, BudgetCurrencyRequests.Eur, 63.0000m, "2026-10-31");
            await BudgetCurrencySteps.AddSharedExpenseAsync(_app, group, 2_000, BudgetCurrencyRequests.Eur, 64.0000m, "2026-11-01");
            await BudgetCurrencySteps.SetAsync(me, BudgetCurrencyRequests.Mkd);

            JsonElement september = await BudgetCurrencySteps.ConvertedTotalsAsync(me.Client, "2026-09");
            JsonElement october = await BudgetCurrencySteps.ConvertedTotalsAsync(me.Client, "2026-10");
            JsonElement november = await BudgetCurrencySteps.ConvertedTotalsAsync(me.Client, "2026-11");

            BudgetCurrencySteps.AssertConvertedTotals(september, BudgetCurrencyRequests.Mkd, 61_000, 0, 61_000, -122_000);
            BudgetCurrencySteps.AssertConvertedTotals(october, BudgetCurrencyRequests.Mkd, 62_000, 0, 125_000, -187_000);
            BudgetCurrencySteps.AssertConvertedTotals(november, BudgetCurrencyRequests.Mkd, 0, 0, 64_000, -64_000);
        }

        [Fact]
        public async Task Month_AnotherPersonsSetting_DoesNotAffectMine()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetCurrencySteps.AddSharedExpenseAsync(_app, group, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetCurrencySteps.SetAsync(group.Member, BudgetCurrencyRequests.Eur);

            JsonElement ownersBefore = await BudgetCurrencySteps.ConvertedTotalsAsync(group.Owner.Client);
            await BudgetCurrencySteps.SetAsync(group.Owner, BudgetCurrencyRequests.Mkd);
            JsonElement members = await BudgetCurrencySteps.ConvertedTotalsAsync(group.Member.Client);
            JsonElement owners = await BudgetCurrencySteps.ConvertedTotalsAsync(group.Owner.Client);

            Assert.Equal(JsonValueKind.Null, ownersBefore.ValueKind);
            BudgetCurrencySteps.AssertConvertedTotals(members, BudgetCurrencyRequests.Eur, 0, 0, 820, -820);
            BudgetCurrencySteps.AssertConvertedTotals(owners, BudgetCurrencyRequests.Mkd, 0, 0, 50_000, -50_000);
        }

        [Fact]
        public async Task Month_AnotherPersonsEntries_AreNeverConverted()
        {
            SignedInUser me = await _app.SignUpAsync();
            SignedInUser stranger = await _app.SignUpAsync("Bojan");
            await BudgetCurrencySteps.AddSpendingAsync(_app, stranger, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            await BudgetCurrencySteps.AddIncomeAsync(_app, stranger, 10_000, BudgetCurrencyRequests.Eur, 61.8123m);
            await BudgetCurrencySteps.SetAsync(me, BudgetCurrencyRequests.Mkd);

            JsonElement convertedTotals = await BudgetCurrencySteps.ConvertedTotalsAsync(me.Client);

            BudgetCurrencySteps.AssertConvertedTotals(convertedTotals, BudgetCurrencyRequests.Mkd, 0, 0, 0, 0);
        }

        [Fact]
        public async Task Month_ByAGuest_Answers403GuestNotAllowed()
        {
            (ExpenseGroup _, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");

            HttpResponseMessage response = await PersonalEntryRequests.ListAsync(guest.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
        }
    }
}
