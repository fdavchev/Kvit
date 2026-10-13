using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Balances
{
    public class GetBalancesPaidUpThroughSettlementsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_EveryWhoPaysWhomPaymentSettledAsASettlement_AnswersAllZeroNoPaymentsAndEveryoneIsKvit()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            await SettleEveryMkdPaymentAsync(group);
            await SettleEveryEurPaymentAsync(group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal(["MKD", "EUR"], BalanceJson.CurrencyCodesOf(body));
            Assert.All(
                body.GetProperty("currencies").EnumerateArray(),
                currencyEntry =>
                {
                    Assert.Equal([0L, 0L, 0L, 0L], BalanceJson.BalanceMinors(currencyEntry));
                    Assert.Empty(BalanceJson.PaymentsOf(currencyEntry));
                });
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_AfterTheFirstMkdPaymentIsSettled_AnswersTheHandCheckedBalancesAndTheTwoRemainingPayments()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.PlainRowIds[0], group.MemberRowId, 53_300);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal([-23_300L, 13_300L, 0L, 10_000L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal(
                [
                    new ExpectedPayment(group.OwnerRowId, group.MemberRowId, 13_300),
                    new ExpectedPayment(group.OwnerRowId, group.PlainRowIds[1], 10_000),
                ],
                BalanceJson.PaymentsOf(mkd));
            Assert.Equal(BalanceScenario.EurBalancesInJoiningOrder, BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "EUR")));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_AfterTwoMkdPaymentsAreSettledAndTheOwnerPaysThePlainNameAtOnce_AnswersZeroMkdBalancesAndIsNotKvitBecauseEurIsOpen()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.PlainRowIds[0], group.MemberRowId, 53_300);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId, 13_300);

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.OwnerRowId, group.PlainRowIds[1], 10_000);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal([0L, 0L, 0L, 0L], BalanceJson.BalanceMinors(mkd));
            Assert.Empty(BalanceJson.PaymentsOf(mkd));
            Assert.Equal(BalanceScenario.EurBalancesInJoiningOrder, BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "EUR")));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_WhenTheOwnerRecordsTheLastPaymentToAPlainName_EveryoneBecomesKvitAtOnce()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);
            await SettleEveryEurPaymentAsync(group);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.PlainRowIds[0], group.MemberRowId, 53_300);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId, 13_300);

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.OwnerRowId, group.PlainRowIds[1], 10_000);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        private static async Task SettleEveryMkdPaymentAsync(ExpenseGroup group)
        {
            Guid ana = group.OwnerRowId;
            Guid bojan = group.MemberRowId;
            Guid marko = group.PlainRowIds[0];
            Guid cveta = group.PlainRowIds[1];

            await SettlementSteps.RecordAsync(group.Member, group.GroupId, marko, bojan, 53_300);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, ana, bojan, 13_300);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, ana, cveta, 10_000);
        }

        private static async Task SettleEveryEurPaymentAsync(ExpenseGroup group)
        {
            Guid ana = group.OwnerRowId;
            Guid bojan = group.MemberRowId;
            Guid marko = group.PlainRowIds[0];
            Guid cveta = group.PlainRowIds[1];

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, marko, ana, 1_667, ExpenseInputs.Eur);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, bojan, cveta, 333, ExpenseInputs.Eur);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, marko, cveta, 333, ExpenseInputs.Eur);
        }
    }
}
