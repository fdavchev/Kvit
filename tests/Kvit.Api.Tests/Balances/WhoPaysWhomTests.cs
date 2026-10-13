using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Xunit;

namespace Kvit.Api.Tests.Balances
{
    public class WhoPaysWhomTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Payments_MixedExpenses_AreTheExpectedMkdList()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(BalanceScenario.ExpectedMkdPayments(group), BalanceJson.PaymentsOf(BalanceJson.CurrencyOf(body, "MKD")));
        }

        [Fact]
        public async Task Payments_MixedExpenses_AreTheExpectedEurListAndNeverMixedWithMkd()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(BalanceScenario.ExpectedEurPayments(group), BalanceJson.PaymentsOf(BalanceJson.CurrencyOf(body, "EUR")));
        }

        [Fact]
        public async Task Payments_PayingTheWholeList_BringsEveryoneToZeroInEveryCurrency()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.All(body.GetProperty("currencies").EnumerateArray(), currencyEntry =>
            {
                Dictionary<Guid, long> remaining = [];
                List<Guid> memberIds = BalanceJson.BalanceMemberIds(currencyEntry);
                List<long> balances = BalanceJson.BalanceMinors(currencyEntry);
                for (int index = 0; index < memberIds.Count; index++)
                {
                    remaining[memberIds[index]] = balances[index];
                }

                foreach (ExpectedPayment payment in BalanceJson.PaymentsOf(currencyEntry))
                {
                    remaining[payment.FromMemberId] += payment.AmountMinor;
                    remaining[payment.ToMemberId] -= payment.AmountMinor;
                }

                Assert.All(remaining.Values, balance => Assert.Equal(0, balance));
            });
        }

        [Fact]
        public async Task Payments_EachCurrency_HasAtMostOneFewerPaymentsThanMembersAndEveryAmountIsPositive()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.All(body.GetProperty("currencies").EnumerateArray(), currencyEntry =>
            {
                List<ExpectedPayment> payments = BalanceJson.PaymentsOf(currencyEntry);
                Assert.InRange(payments.Count, 1, BalanceJson.BalanceMemberIds(currencyEntry).Count - 1);
                Assert.All(payments, payment => Assert.True(payment.AmountMinor > 0));
                Assert.All(payments, payment => Assert.NotEqual(payment.FromMemberId, payment.ToMemberId));
                Assert.All(payments, payment => Assert.Contains(payment.FromMemberId, BalanceJson.BalanceMemberIds(currencyEntry)));
                Assert.All(payments, payment => Assert.Contains(payment.ToMemberId, BalanceJson.BalanceMemberIds(currencyEntry)));
            });
        }

        [Fact]
        public async Task Payments_SeveralPeopleOweTheSameAmount_TheEarliestJoinerPaysFirst()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(40_000, ExpenseInputs.Mkd, group.OwnerRowId, [.. group.RowIdsInJoiningOrder]));

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            List<ExpectedPayment> expected =
            [
                new ExpectedPayment(group.MemberRowId, group.OwnerRowId, 10_000),
                new ExpectedPayment(group.PlainRowIds[0], group.OwnerRowId, 10_000),
                new ExpectedPayment(group.PlainRowIds[1], group.OwnerRowId, 10_000),
            ];
            Assert.Equal([30_000L, -10_000L, -10_000L, -10_000L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.Equal(expected, BalanceJson.PaymentsOf(BalanceJson.CurrencyOf(body, "MKD")));
        }

        [Fact]
        public async Task Payments_SeveralPeopleAreOwedTheSameAmount_TheEarliestJoinerIsPaidFirst()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            Guid ana = group.OwnerRowId;
            Guid bojan = group.MemberRowId;
            Guid marko = group.PlainRowIds[0];
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Of("Exact", 10_000, ExpenseInputs.Mkd, ana, new ShareInput(marko, 10_000)));
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Of("Exact", 10_000, ExpenseInputs.Mkd, bojan, new ShareInput(marko, 10_000)));

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            List<ExpectedPayment> expected =
            [
                new ExpectedPayment(marko, ana, 10_000),
                new ExpectedPayment(marko, bojan, 10_000),
            ];
            Assert.Equal([10_000L, 10_000L, -20_000L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.Equal(expected, BalanceJson.PaymentsOf(BalanceJson.CurrencyOf(body, "MKD")));
        }

        [Fact]
        public async Task Payments_RepeatedCalls_AnswerTheIdenticalText()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            string first = await BalanceRequests.ReadTextAsync(group.Owner.Client, group.GroupId);
            string second = await BalanceRequests.ReadTextAsync(group.Owner.Client, group.GroupId);
            string third = await BalanceRequests.ReadTextAsync(group.Member.Client, group.GroupId);

            Assert.Equal(first, second);
            Assert.Equal(first, third);
        }

        [Fact]
        public async Task Payments_EveryoneAlreadyEven_AreEmpty()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId));

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal([0L, 0L], BalanceJson.BalanceMinors(mkd));
            Assert.Empty(BalanceJson.PaymentsOf(mkd));
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }
    }
}
