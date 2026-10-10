using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Balances
{
    public class GetBalancesFormerMembersTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_RemovedAndLeftMembersWhoAreInOldExpenses_AreStillInTheBalancesAsNotCurrent()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await AddOldExpensesAndEndTwoMembersAsync(group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal(group.RowIdsInJoiningOrder, BalanceJson.BalanceMemberIds(mkd));
            Assert.Equal([10_000L, -50_000L, 90_000L, -50_000L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal([true, false, false, true], BalanceJson.BalanceIsCurrents(mkd));
        }

        [Fact]
        public async Task Get_RemovedAndLeftMembers_KeepTheirNames()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await AddOldExpensesAndEndTwoMembersAsync(group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["Ana", "Bojan", "Marko", "Cveta"], BalanceJson.BalanceNames(BalanceJson.CurrencyOf(body, "MKD")));
        }

        [Fact]
        public async Task Get_RemovedAndLeftMembers_StillOweAndAreOwedInThePaymentList()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await AddOldExpensesAndEndTwoMembersAsync(group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            List<ExpectedPayment> expected =
            [
                new ExpectedPayment(group.MemberRowId, group.PlainRowIds[0], 50_000),
                new ExpectedPayment(group.PlainRowIds[1], group.PlainRowIds[0], 40_000),
                new ExpectedPayment(group.PlainRowIds[1], group.OwnerRowId, 10_000),
            ];
            Assert.Equal(expected, BalanceJson.PaymentsOf(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_RemovedMemberWhoIsInNoExpense_IsInTheAnswerWithZeroAndNotCurrent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal(group.RowIdsInJoiningOrder, BalanceJson.BalanceMemberIds(mkd));
            Assert.Equal([50_000L, -50_000L, 0L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal([true, true, false], BalanceJson.BalanceIsCurrents(mkd));
        }

        [Fact]
        public async Task Get_MemberWhoLeft_AppearsInBothCurrenciesAsNotCurrent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId));
            await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(1_000, ExpenseInputs.Eur, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Left");

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["MKD", "EUR"], BalanceJson.CurrencyCodesOf(body));
            Assert.Equal([-50_000L, 50_000L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.Equal([500L, -500L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "EUR")));
            Assert.All(body.GetProperty("currencies").EnumerateArray(), currencyEntry => Assert.Equal([true, false], BalanceJson.BalanceIsCurrents(currencyEntry)));
        }

        private async Task AddOldExpensesAndEndTwoMembersAsync(ExpenseGroup group)
        {
            Guid ana = group.OwnerRowId;
            Guid bojan = group.MemberRowId;
            Guid marko = group.PlainRowIds[0];
            Guid cveta = group.PlainRowIds[1];
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, marko, ana, bojan, marko, cveta));
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(60_000, ExpenseInputs.Mkd, ana, ana, bojan, cveta));
            await ExpenseRows.EndMemberRowAsync(_app, marko, group.Owner.UserId);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Left");
        }
    }
}
