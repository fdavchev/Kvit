using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Balances
{
    public class GetBalancesSetAsideRowsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_MemberWhoClaimedAPlainName_AppearsOnlyOnceAsTheCurrentNameRow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid marko = group.PlainRowIds[0];
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(60_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, marko));
            await ClaimAsync(group.Member, group.GroupId, marko);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, ExpenseInputs.Mkd);
            Assert.Equal([group.OwnerRowId, marko], BalanceJson.BalanceMemberIds(mkd));
            Assert.Equal(["Ana", "Bojan"], BalanceJson.BalanceNames(mkd));
            Assert.Equal([30_000L, -30_000L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal([true, true], BalanceJson.BalanceIsCurrents(mkd));
            Assert.DoesNotContain(group.MemberRowId, BalanceJson.BalanceMemberIds(mkd));
        }

        [Fact]
        public async Task Get_MemberWhoClaimedAPlainName_KeepsTheNameRowInThePaymentList()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid marko = group.PlainRowIds[0];
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(60_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, marko));
            await ClaimAsync(group.Member, group.GroupId, marko);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Member.Client, group.GroupId);

            Assert.Equal([new ExpectedPayment(marko, group.OwnerRowId, 30_000)], BalanceJson.PaymentsOf(BalanceJson.CurrencyOf(body, ExpenseInputs.Mkd)));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_MemberWhoClaimedAPlainName_HidesTheSetAsideRowInEveryCurrency()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid marko = group.PlainRowIds[0];
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(60_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, marko));
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(2_000, ExpenseInputs.Eur, marko, group.OwnerRowId, marko));
            await ClaimAsync(group.Member, group.GroupId, marko);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["MKD", "EUR"], BalanceJson.CurrencyCodesOf(body));
            Assert.All(body.GetProperty("currencies").EnumerateArray(), currencyEntry => Assert.Equal([group.OwnerRowId, marko], BalanceJson.BalanceMemberIds(currencyEntry)));
            Assert.Equal([-1_000L, 1_000L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, ExpenseInputs.Eur)));
        }

        [Fact]
        public async Task Get_TwoMembersWhoClaimedNames_HideBothSetAsideRows()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MarkoRowId, group.CvetaRowId));
            await ClaimAsync(group.Member, group.GroupId, group.MarkoRowId);
            await ClaimAsync(group.Third, group.GroupId, group.CvetaRowId);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, ExpenseInputs.Mkd);
            Assert.Equal([group.OwnerRowId, group.MarkoRowId, group.CvetaRowId], BalanceJson.BalanceMemberIds(mkd));
            Assert.Equal(["Ana", "Bojan", "Dime"], BalanceJson.BalanceNames(mkd));
            Assert.Equal([80_000L, -40_000L, -40_000L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal([true, true, true], BalanceJson.BalanceIsCurrents(mkd));
        }

        [Fact]
        public async Task Get_LeftAndRemovedMembersWithOldExpenses_StayWhileTheSetAsideRowIsHidden()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(100_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MarkoRowId, group.CvetaRowId, group.ThirdRowId));
            await ClaimAsync(group.Member, group.GroupId, group.MarkoRowId);
            await ExpenseRows.EndMemberRowAsync(_app, group.CvetaRowId, group.Owner.UserId);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Third.UserId, "Left");

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, ExpenseInputs.Mkd);
            Assert.Equal([group.OwnerRowId, group.MarkoRowId, group.CvetaRowId, group.ThirdRowId], BalanceJson.BalanceMemberIds(mkd));
            Assert.Equal(["Ana", "Bojan", "Cveta", "Dime"], BalanceJson.BalanceNames(mkd));
            Assert.Equal([75_000L, -25_000L, -25_000L, -25_000L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal([true, true, false, false], BalanceJson.BalanceIsCurrents(mkd));
        }

        [Fact]
        public async Task Get_AfterTheOwnerUndoesTheClaim_ShowsBothTheRestoredOwnRowAndTheNameRowAgain()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid marko = group.PlainRowIds[0];
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(60_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, marko));
            await ClaimAsync(group.Member, group.GroupId, marko);
            HttpResponseMessage undo = await GroupRequests.UndoClaimAsync(group.Owner.Client, group.GroupId, marko);
            Assert.Equal(HttpStatusCode.NoContent, undo.StatusCode);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, ExpenseInputs.Mkd);
            Assert.Equal([group.OwnerRowId, group.MemberRowId, marko], BalanceJson.BalanceMemberIds(mkd));
            Assert.Equal(["Ana", "Bojan", "Marko"], BalanceJson.BalanceNames(mkd));
            Assert.Equal([30_000L, 0L, -30_000L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal([true, true, true], BalanceJson.BalanceIsCurrents(mkd));
        }

        [Fact]
        public async Task Get_MemberWhoDidNotClaimAnything_StillAppearsWithTheirOwnRow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(60_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Member.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, ExpenseInputs.Mkd);
            Assert.Equal(group.RowIdsInJoiningOrder, BalanceJson.BalanceMemberIds(mkd));
            Assert.Equal([30_000L, -30_000L, 0L], BalanceJson.BalanceMinors(mkd));
        }

        private static async Task ClaimAsync(SignedInUser claimer, Guid groupId, Guid nameRowId)
        {
            HttpResponseMessage response = await GroupRequests.ClaimAsync(claimer.Client, groupId, nameRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
