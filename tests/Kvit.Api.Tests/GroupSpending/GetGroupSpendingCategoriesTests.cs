using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Xunit;

namespace Kvit.Api.Tests.GroupSpending
{
    public class GetGroupSpendingCategoriesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_TwoExpensesInOneCategory_AddUpInOneRow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, group, 25_000, categoryId: GroupSpendingSteps.Food);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Member.Client, group.GroupId);

            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Food, 125_000)], GroupSpendingJson.CategoriesOf(body, "MKD"));
        }

        [Fact]
        public async Task Get_Categories_AnswerInTheBuiltInOrderWhateverOrderTheyWereAdded()
        {
            Guid other = await _app.BuiltInCategoryIdAsync("other");
            Guid groceries = await _app.BuiltInCategoryIdAsync("groceries");
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 40_000, categoryId: other);
            await GroupSpendingSteps.AddAsync(_app, group, 30_000, categoryId: GroupSpendingSteps.Transport);
            await GroupSpendingSteps.AddAsync(_app, group, 20_000, categoryId: groceries);
            await GroupSpendingSteps.AddAsync(_app, group, 10_000, categoryId: GroupSpendingSteps.Food);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(
                [
                    new CategoryTotal(GroupSpendingSteps.Food, 10_000),
                    new CategoryTotal(groceries, 20_000),
                    new CategoryTotal(GroupSpendingSteps.Transport, 30_000),
                    new CategoryTotal(other, 40_000),
                ],
                GroupSpendingJson.CategoriesOf(body, "MKD"));
        }

        [Fact]
        public async Task Get_ExpensesWithoutACategory_AreTheLastRowEvenWhenAddedFirst()
        {
            Guid other = await _app.BuiltInCategoryIdAsync("other");
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 50_000);
            await GroupSpendingSteps.AddAsync(_app, group, 40_000, categoryId: other);
            await GroupSpendingSteps.AddAsync(_app, group, 10_000, categoryId: GroupSpendingSteps.Food);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(
                [
                    new CategoryTotal(GroupSpendingSteps.Food, 10_000),
                    new CategoryTotal(other, 40_000),
                    new CategoryTotal(null, 50_000),
                ],
                GroupSpendingJson.CategoriesOf(body, "MKD"));
        }

        [Fact]
        public async Task Get_TwoExpensesWithoutACategory_AddUpInOneRowWithANullCategoryId()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 50_000);
            await GroupSpendingSteps.AddAsync(_app, group, 7_000);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            JsonElement row = Assert.Single(GroupSpendingJson.CurrencyOf(body, "MKD").GetProperty("categories").EnumerateArray());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("categoryId").ValueKind);
            Assert.Equal(57_000, row.GetProperty("totalMinor").GetInt64());
        }

        [Fact]
        public async Task Get_CategoryWithoutExpensesInThatCurrency_IsNotListedThere()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 30_000, categoryId: GroupSpendingSteps.Transport);
            await GroupSpendingSteps.AddAsync(_app, group, 1_000, ExpenseInputs.Eur, GroupSpendingSteps.Food);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Transport, 30_000)], GroupSpendingJson.CategoriesOf(body, "MKD"));
            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Food, 1_000)], GroupSpendingJson.CategoriesOf(body, "EUR"));
        }

        [Fact]
        public async Task Get_SameCategoryInBothCurrencies_KeepsTheTwoCurrenciesApart()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, group, 1_000, ExpenseInputs.Eur, GroupSpendingSteps.Food);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Food, ExpenseInputs.OneThousandMkd)], GroupSpendingJson.CategoriesOf(body, "MKD"));
            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Food, 1_000)], GroupSpendingJson.CategoriesOf(body, "EUR"));
        }

        [Fact]
        public async Task Get_EachCurrency_HasItsOwnOrderWithNoCategoryLast()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 5_000, ExpenseInputs.Eur);
            await GroupSpendingSteps.AddAsync(_app, group, 4_000, ExpenseInputs.Eur, GroupSpendingSteps.Transport);
            await GroupSpendingSteps.AddAsync(_app, group, 3_000, ExpenseInputs.Eur, GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, group, 90_000, categoryId: GroupSpendingSteps.Transport);
            await GroupSpendingSteps.AddAsync(_app, group, 80_000);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(
                [new CategoryTotal(GroupSpendingSteps.Transport, 90_000), new CategoryTotal(null, 80_000)],
                GroupSpendingJson.CategoriesOf(body, "MKD"));
            Assert.Equal(
                [new CategoryTotal(GroupSpendingSteps.Food, 3_000), new CategoryTotal(GroupSpendingSteps.Transport, 4_000), new CategoryTotal(null, 5_000)],
                GroupSpendingJson.CategoriesOf(body, "EUR"));
        }

        [Fact]
        public async Task Get_CategoryRows_AddUpToTheCurrencyTotal()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 12_300, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, group, 67_800, categoryId: GroupSpendingSteps.Transport);
            await GroupSpendingSteps.AddAsync(_app, group, 1_100);
            await GroupSpendingSteps.AddAsync(_app, group, 2_222, ExpenseInputs.Eur, GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, group, 333, ExpenseInputs.Eur);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            foreach (string currency in GroupSpendingJson.CurrencyCodesOf(body))
            {
                Assert.Equal(GroupSpendingJson.TotalOf(body, currency), GroupSpendingJson.CategoriesOf(body, currency).Sum(row => row.TotalMinor));
            }
        }
    }
}
