using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Xunit;

namespace Kvit.Api.Tests.GroupSpending
{
    public class GetGroupSpendingTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_GroupWithoutExpenses_AnswersNoCurrencies()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(JsonValueKind.Array, body.GetProperty("currencies").ValueKind);
            Assert.Equal(0, body.GetProperty("currencies").GetArrayLength());
        }

        [Fact]
        public async Task Get_GroupWithoutExpenses_AnswersAPresentNullPlan()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.True(body.TryGetProperty("plan", out JsonElement plan));
            Assert.Equal(JsonValueKind.Null, plan.ValueKind);
        }

        [Fact]
        public async Task Get_GroupWithExpensesAndNoPlan_AnswersAPresentNullPlan()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.True(body.TryGetProperty("plan", out JsonElement plan));
            Assert.Equal(JsonValueKind.Null, plan.ValueKind);
        }

        [Fact]
        public async Task Get_Answer_HasExactlyTheCurrenciesAndPlanFields()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(GroupSpendingJson.ResponseFieldNames, GroupSpendingJson.PropertyNamesOf(body));
        }

        [Fact]
        public async Task Get_Currency_HasExactlyTheCurrencyTotalAndCategoriesFields()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(GroupSpendingJson.CurrencyFieldNames, GroupSpendingJson.PropertyNamesOf(GroupSpendingJson.CurrencyOf(body, "MKD")));
        }

        [Fact]
        public async Task Get_CategoryRow_HasExactlyTheCategoryIdAndTotalFields()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.TwelveHundredMkd);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            JsonElement[] rows = [.. GroupSpendingJson.CurrencyOf(body, "MKD").GetProperty("categories").EnumerateArray()];
            Assert.Equal(2, rows.Length);
            Assert.All(rows, row => Assert.Equal(GroupSpendingJson.CategoryFieldNames, GroupSpendingJson.PropertyNamesOf(row)));
        }

        [Fact]
        public async Task Get_OnlyMkdExpenses_AnswersOnlyMkdWithItsTotal()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd);
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.TwelveHundredMkd);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["MKD"], GroupSpendingJson.CurrencyCodesOf(body));
            Assert.Equal(220_000, GroupSpendingJson.TotalOf(body, "MKD"));
        }

        [Fact]
        public async Task Get_OnlyEurExpenses_AnswersOnlyEurWithItsTotal()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 1_000, ExpenseInputs.Eur);
            await GroupSpendingSteps.AddAsync(_app, group, 2_550, ExpenseInputs.Eur);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["EUR"], GroupSpendingJson.CurrencyCodesOf(body));
            Assert.Equal(3_550, GroupSpendingJson.TotalOf(body, "EUR"));
        }

        [Fact]
        public async Task Get_BothCurrencies_AnswersMkdFirstThenEurEvenWhenEurWasAddedFirst()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 1_000, ExpenseInputs.Eur);
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["MKD", "EUR"], GroupSpendingJson.CurrencyCodesOf(body));
        }

        [Fact]
        public async Task Get_BothCurrencies_NeverAddsOrConvertsThem()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd);
            await GroupSpendingSteps.AddAsync(_app, group, 1_000, ExpenseInputs.Eur);
            await GroupSpendingSteps.AddAsync(_app, group, 250_000);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(350_000, GroupSpendingJson.TotalOf(body, "MKD"));
            Assert.Equal(1_000, GroupSpendingJson.TotalOf(body, "EUR"));
        }

        [Fact]
        public async Task Get_ExpenseSplitBetweenThreePeople_CountsTheWholeAmountNotAShare()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]);
            await _app.AddExpenseAsync(group.Member, group.GroupId, input);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(ExpenseInputs.OneThousandMkd, GroupSpendingJson.TotalOf(body, "MKD"));
        }

        [Fact]
        public async Task Get_ExpenseSplitUnevenly_CountsTheWholeAmount()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Of("Exact", 90_000, ExpenseInputs.Mkd, group.MemberRowId, new ShareInput(group.OwnerRowId, 20_000), new ShareInput(group.MemberRowId, 70_000));
            await _app.AddExpenseAsync(group.Member, group.GroupId, input);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(90_000, GroupSpendingJson.TotalOf(body, "MKD"));
        }

        [Fact]
        public async Task Get_AnotherMember_SeesExactlyWhatTheOwnerSees()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, group, 1_000, ExpenseInputs.Eur, GroupSpendingSteps.Transport);

            string forOwner = await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId);
            string forMember = await GroupSpendingRequests.ReadTextAsync(group.Member.Client, group.GroupId);

            Assert.Equal(forOwner, forMember);
        }
    }
}
