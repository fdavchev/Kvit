using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kvit.Api.Tests.Expenses;
using Kvit.Contracts.Balances;
using Kvit.Domain.MoneyRules;
using Xunit;

namespace Kvit.Api.Tests.Balances
{
    public class BalancesContractTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };

        [Fact]
        public async Task Get_Answer_ReadsBackIntoTheBalancesResponseRecords()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            HttpResponseMessage response = await BalanceRequests.GetAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            BalancesResponse body = JsonSerializer.Deserialize<BalancesResponse>(text, WebOptions)!;
            Assert.False(body.IsEveryoneKvit);
            Assert.Equal<Currency>([Currency.MKD, Currency.EUR], body.Currencies.Select(currencyBalances => currencyBalances.Currency));
            CurrencyBalancesResponse mkd = body.Currencies[0];
            Assert.Equal(BalanceScenario.MkdBalancesInJoiningOrder, mkd.Balances.Select(row => row.BalanceMinor));
            Assert.Equal(group.RowIdsInJoiningOrder, mkd.Balances.Select(row => row.MemberId));
            Assert.Equal(["Ana", "Bojan", "Marko", "Cveta"], mkd.Balances.Select(row => row.Name));
            Assert.All(mkd.Balances, row => Assert.True(row.IsCurrent));
            Assert.Equal(BalanceScenario.ExpectedMkdPayments(group), mkd.Payments.Select(payment => new ExpectedPayment(payment.FromMemberId, payment.ToMemberId, payment.AmountMinor)));
        }
    }
}
