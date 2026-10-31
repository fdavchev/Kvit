using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.GroupSpending;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public class GetGroupSpendingPlanSpentTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const decimal RealisticRate = 61.5610m;
        private const long VeryLargePlan = 900_000_000_000;

        [Fact]
        public async Task Get_GroupWithAPlanAndNoExpenses_AnswersTheFullPlanObjectWithZeros()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 100_000, GroupPlanRequests.Mkd);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 100_000, GroupPlanRequests.Mkd, 0, 50_000, 0, 0);
        }

        [Fact]
        public async Task Get_PlanThatWasRemoved_AnswersANullPlanAgain()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 100_000, GroupPlanRequests.Mkd);
            await GroupPlanSteps.RemoveAsync(group.Owner, group.GroupId);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.True(body.TryGetProperty("plan", out JsonElement plan));
            Assert.Equal(JsonValueKind.Null, plan.ValueKind);
        }

        [Fact]
        public async Task Get_PlanSetThroughTheEndpoint_IsTheOneTheSpendingAnswerShows()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 40_000);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 100_000, GroupPlanRequests.Mkd);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Member.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 100_000, GroupPlanRequests.Mkd, 40_000, 50_000, 0, 0);
        }

        [Fact]
        public async Task Get_MkdPlanWithMkdExpenses_CountsThemUntouchedAndIncludesNothingConverted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            await GroupPlanSteps.AddAtRateAsync(_app, group, ExpenseInputs.OneThousandMkd, GroupPlanRequests.Mkd, RealisticRate);
            await GroupPlanSteps.AddAtRateAsync(_app, group, ExpenseInputs.TwelveHundredMkd, GroupPlanRequests.Mkd, 62.0000m);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 500_000, GroupPlanRequests.Mkd, 220_000, 250_000, 0, 0);
        }

        [Fact]
        public async Task Get_EurPlanWithEurExpenses_CountsThemUntouchedAndIncludesNothingConverted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 10_000, GroupPlanRequests.Eur);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, RealisticRate);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 2_550, GroupPlanRequests.Eur, 62.0000m);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 10_000, GroupPlanRequests.Eur, 3_550, 5_000, 0, 0);
        }

        [Fact]
        public async Task Get_MkdPlanWithAnEurExpense_ConvertsItWithItsSavedRateToAWholeDenarAndReportsTheEurCents()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, RealisticRate);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 500_000, GroupPlanRequests.Mkd, 61_600, 250_000, 0, 1_000);
        }

        [Fact]
        public async Task Get_EurPlanWithAnMkdExpense_ConvertsItWithItsSavedRateToACentAndReportsTheDeni()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 10_000, GroupPlanRequests.Eur);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 100_000, GroupPlanRequests.Mkd, RealisticRate);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 10_000, GroupPlanRequests.Eur, 1_624, 5_000, 0, 100_000);
        }

        [Fact]
        public async Task Get_MkdPlanWithBothCurrencies_AddsTheUntouchedMkdToTheConvertedEur()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            await GroupPlanSteps.AddAtRateAsync(_app, group, ExpenseInputs.OneThousandMkd, GroupPlanRequests.Mkd, 62.0000m);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, RealisticRate);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 500_000, GroupPlanRequests.Mkd, 161_600, 250_000, 0, 1_000);
        }

        [Fact]
        public async Task Get_EurPlanWithBothCurrencies_AddsTheUntouchedEurToTheConvertedMkd()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 10_000, GroupPlanRequests.Eur);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, 62.0000m);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 100_000, GroupPlanRequests.Mkd, RealisticRate);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 10_000, GroupPlanRequests.Eur, 2_624, 5_000, 0, 100_000);
        }

        [Fact]
        public async Task Get_EurExpensesSavedWithDifferentRates_EachUsesItsOwnRate()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, 61.0000m);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, 62.0000m);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 500_000, GroupPlanRequests.Mkd, 123_000, 250_000, 0, 2_000);
        }

        [Fact]
        public async Task Get_MkdExpensesSavedWithDifferentRates_EachUsesItsOwnRate()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 10_000, GroupPlanRequests.Eur);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 61_000, GroupPlanRequests.Mkd, 61.0000m);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 62_000, GroupPlanRequests.Mkd, 62.0000m);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 10_000, GroupPlanRequests.Eur, 2_000, 5_000, 0, 123_000);
        }

        [Fact]
        public async Task Get_RateOfALaterExpense_DoesNotChangeTheRateSavedWithAnEarlierOne()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, 61.0000m);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, 70.0000m);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(131_000, plan.GetProperty("spentMinor").GetInt64());
        }

        [Theory]
        [InlineData(100, 62.4900, 6_200)]
        [InlineData(100, 62.5000, 6_300)]
        [InlineData(100, 62.5100, 6_300)]
        [InlineData(150, 61.0000, 9_200)]
        [InlineData(250, 61.5000, 15_400)]
        [InlineData(1, 61.5610, 100)]
        [InlineData(1, 40.0000, 0)]
        public async Task Get_EurExpenseInAnMkdPlan_RoundsToTheNearestWholeDenarWithHalvesAwayFromZero(long eurCents, double rate, long expectedDeni)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            await GroupPlanSteps.AddAtRateAsync(_app, group, eurCents, GroupPlanRequests.Eur, (decimal)rate);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(expectedDeni, plan.GetProperty("spentMinor").GetInt64());
            Assert.Equal(eurCents, plan.GetProperty("includedOtherCurrencyMinor").GetInt64());
        }

        [Theory]
        [InlineData(800, 64.0000, 13)]
        [InlineData(700, 64.0000, 11)]
        [InlineData(100, 64.0000, 2)]
        [InlineData(100, 200.0000, 1)]
        [InlineData(100, 201.0000, 0)]
        [InlineData(100_000, 61.5610, 1_624)]
        public async Task Get_MkdExpenseInAnEurPlan_RoundsToTheNearestCentWithHalvesAwayFromZero(long deni, double rate, long expectedCents)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 10_000, GroupPlanRequests.Eur);
            await GroupPlanSteps.AddAtRateAsync(_app, group, deni, GroupPlanRequests.Mkd, (decimal)rate);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(expectedCents, plan.GetProperty("spentMinor").GetInt64());
            Assert.Equal(deni, plan.GetProperty("includedOtherCurrencyMinor").GetInt64());
        }

        [Fact]
        public async Task Get_TwoEurExpensesInAnMkdPlan_AreConvertedOneByOneAndThenAdded()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 100, GroupPlanRequests.Eur, 62.5000m);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 100, GroupPlanRequests.Eur, 62.5000m);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 500_000, GroupPlanRequests.Mkd, 12_600, 250_000, 0, 200);
        }

        [Fact]
        public async Task Get_TwoMkdExpensesInAnEurPlan_AreConvertedOneByOneAndThenAdded()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 10_000, GroupPlanRequests.Eur);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 800, GroupPlanRequests.Mkd, 64.0000m);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 800, GroupPlanRequests.Mkd, 64.0000m);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 10_000, GroupPlanRequests.Eur, 26, 5_000, 0, 1_600);
        }

        [Fact]
        public async Task Get_EurExpenseAtTheEntryBoundInAnMkdPlan_ConvertsExactlyWithoutOverflow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, GroupPlanRequests.LargestMkdAmount, GroupPlanRequests.Mkd);
            await GroupPlanSteps.AddAtRateAsync(_app, group, GroupSpendingSteps.EurBound, GroupPlanRequests.Eur, RealisticRate);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(61_560_999_999_900, plan.GetProperty("spentMinor").GetInt64());
            Assert.Equal(GroupSpendingSteps.EurBound, plan.GetProperty("includedOtherCurrencyMinor").GetInt64());
            Assert.Equal(61_560_999_999_900 - GroupPlanRequests.LargestMkdAmount, plan.GetProperty("overMinor").GetInt64());
        }

        [Fact]
        public async Task Get_ThreeExpensesAtTheEntryBoundInThePlanCurrency_SumWithoutOverflow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, VeryLargePlan, GroupPlanRequests.Mkd);
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, GroupSpendingSteps.MkdBound);
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, GroupSpendingSteps.MkdBound);
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, GroupSpendingSteps.MkdBound);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(2_999_999_999_700, plan.GetProperty("spentMinor").GetInt64());
            Assert.Equal(2_999_999_999_700 - VeryLargePlan, plan.GetProperty("overMinor").GetInt64());
        }

        [Fact]
        public async Task Get_SpentExactlyAtThePlan_HasNoOver()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 100_000, GroupPlanRequests.Mkd);
            await GroupSpendingSteps.AddAsync(_app, group, 100_000);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 100_000, GroupPlanRequests.Mkd, 100_000, 50_000, 0, 0);
        }

        [Fact]
        public async Task Get_SpentOneDenarAboveAnMkdPlan_ShowsThatDenarAsOver()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 100_000, GroupPlanRequests.Mkd);
            await GroupSpendingSteps.AddAsync(_app, group, 100_100);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 100_000, GroupPlanRequests.Mkd, 100_100, 50_000, 100, 0);
        }

        [Theory]
        [InlineData(999, 0)]
        [InlineData(1_000, 0)]
        [InlineData(1_001, 1)]
        [InlineData(5_000, 4_000)]
        public async Task Get_EurPlan_ShowsOnlyTheCentsAboveThePlanAsOver(long spentCents, long expectedOver)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 1_000, GroupPlanRequests.Eur);
            await GroupSpendingSteps.AddAsync(_app, group, spentCents, GroupPlanRequests.Eur);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(spentCents, plan.GetProperty("spentMinor").GetInt64());
            Assert.Equal(expectedOver, plan.GetProperty("overMinor").GetInt64());
        }

        [Fact]
        public async Task Get_ConvertedSpendingAbovePlan_ShowsTheConvertedExcessAsOver()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 50_000, GroupPlanRequests.Mkd);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, RealisticRate);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 50_000, GroupPlanRequests.Mkd, 61_600, 25_000, 11_600, 1_000);
        }

        [Fact]
        public async Task Get_DeletedExpense_IsLeftOutOfTheSpentAndOfTheConvertedPart()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            await GroupSpendingSteps.AddAsync(_app, group, 100_000);
            Guid deletedEur = await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, RealisticRate);
            Guid deletedMkd = await GroupSpendingSteps.AddAsync(_app, group, 50_000);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, deletedEur);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, deletedMkd);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 500_000, GroupPlanRequests.Mkd, 100_000, 250_000, 0, 0);
        }

        [Fact]
        public async Task Get_DeletedThenRestoredExpense_IsCountedAgain()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            Guid expenseId = await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, RealisticRate);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 500_000, GroupPlanRequests.Mkd, 61_600, 250_000, 0, 1_000);
        }

        [Fact]
        public async Task Get_Settlements_NeverCountAsSpending()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            await GroupSpendingSteps.AddAsync(_app, group, 100_000);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 50_000, ExpenseInputs.Mkd);

            JsonElement pending = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);
            JsonElement confirmed = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(pending, 500_000, GroupPlanRequests.Mkd, 100_000, 250_000, 0, 0);
            GroupPlanSteps.AssertPlan(confirmed, 500_000, GroupPlanRequests.Mkd, 100_000, 250_000, 0, 0);
        }

        [Fact]
        public async Task Get_EurSettlementInAnMkdPlan_NeverCountsAsConvertedSpending()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, RealisticRate);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 500, ExpenseInputs.Eur);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 500_000, GroupPlanRequests.Mkd, 61_600, 250_000, 0, 1_000);
        }

        [Fact]
        public async Task Get_ExpenseOfAnotherGroup_NeverCounts()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseGroup other = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            await GroupSpendingSteps.AddAsync(_app, other, 300_000);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(plan, 500_000, GroupPlanRequests.Mkd, 0, 250_000, 0, 0);
        }

        [Fact]
        public async Task Get_ChangedPlanCurrency_ConvertsTheSameExpensesTheOtherWay()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, RealisticRate);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 100_000, GroupPlanRequests.Mkd, RealisticRate);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            JsonElement inMkd = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 10_000, GroupPlanRequests.Eur);
            JsonElement inEur = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            GroupPlanSteps.AssertPlan(inMkd, 500_000, GroupPlanRequests.Mkd, 161_600, 250_000, 0, 1_000);
            GroupPlanSteps.AssertPlan(inEur, 10_000, GroupPlanRequests.Eur, 2_624, 5_000, 0, 100_000);
        }

        [Fact]
        public async Task Get_OneBillGroupWithAPlan_CountsTheBillAsSpending()
        {
            SignedInUser owner = await _app.SignUpAsync();
            (Guid groupId, _) = await _app.CreateOneBillAsync(owner, OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko"));
            await GroupPlanSteps.SetAsync(owner, groupId, 150_000, GroupPlanRequests.Mkd);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(owner.Client, groupId);

            GroupPlanSteps.AssertPlan(plan, 150_000, GroupPlanRequests.Mkd, ExpenseInputs.OneThousandMkd, 75_000, 0, 0);
        }
    }
}
