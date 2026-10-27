using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Xunit;

namespace Kvit.Api.Tests.GroupSpending
{
    public class GetGroupSpendingLargeAmountsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_ThreeExpensesAtTheEntryBound_SumInOneCategoryWithoutOverflow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, GroupSpendingSteps.MkdBound, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, GroupSpendingSteps.MkdBound, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, GroupSpendingSteps.MkdBound, categoryId: GroupSpendingSteps.Food);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(2_999_999_999_700, GroupSpendingJson.TotalOf(body, "MKD"));
            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Food, 2_999_999_999_700)], GroupSpendingJson.CategoriesOf(body, "MKD"));
        }

        [Fact]
        public async Task Get_ExpensesAtTheEntryBoundInSeveralCategories_SumTheCurrencyTotal()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, GroupSpendingSteps.MkdBound, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, GroupSpendingSteps.MkdBound, categoryId: GroupSpendingSteps.Transport);
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, GroupSpendingSteps.MkdBound - 100);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(2_999_999_999_600, GroupSpendingJson.TotalOf(body, "MKD"));
            Assert.Equal(
                [
                    new CategoryTotal(GroupSpendingSteps.Food, GroupSpendingSteps.MkdBound),
                    new CategoryTotal(GroupSpendingSteps.Transport, GroupSpendingSteps.MkdBound),
                    new CategoryTotal(null, GroupSpendingSteps.MkdBound - 100),
                ],
                GroupSpendingJson.CategoriesOf(body, "MKD"));
        }

        [Fact]
        public async Task Get_ExpensesAtTheEntryBoundInBothCurrencies_SumEachCurrencyOnItsOwn()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, GroupSpendingSteps.MkdBound);
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, GroupSpendingSteps.MkdBound);
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, GroupSpendingSteps.EurBound, ExpenseInputs.Eur);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(1_999_999_999_800, GroupSpendingJson.TotalOf(body, "MKD"));
            Assert.Equal(GroupSpendingSteps.EurBound, GroupSpendingJson.TotalOf(body, "EUR"));
        }

        [Fact]
        public async Task Get_SumBeyondTheRangeOfAThirtyTwoBitNumber_IsNotCutOff()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, 2_000_000_000);
            await GroupSpendingSteps.AddPaidAloneAsync(_app, group, 2_000_000_100);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(4_000_000_100, GroupSpendingJson.TotalOf(body, "MKD"));
        }
    }
}
