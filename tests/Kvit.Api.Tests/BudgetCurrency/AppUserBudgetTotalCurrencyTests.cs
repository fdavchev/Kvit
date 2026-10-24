using Kvit.Domain.MoneyRules;
using Kvit.Infrastructure.Auth;
using Xunit;

namespace Kvit.Api.Tests.BudgetCurrency
{
    public class AppUserBudgetTotalCurrencyTests
    {
        [Fact]
        public void NewUser_HasNoBudgetTotalCurrency()
        {
            AppUser user = new();

            Assert.Null(user.BudgetTotalCurrency);
        }

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void SetBudgetTotalCurrency_ACurrency_IsKept(Currency currency)
        {
            AppUser user = new();

            user.SetBudgetTotalCurrency(currency);

            Assert.Equal(currency, user.BudgetTotalCurrency);
        }

        [Fact]
        public void SetBudgetTotalCurrency_MkdThenEur_KeepsTheLastOne()
        {
            AppUser user = new();
            user.SetBudgetTotalCurrency(Currency.MKD);

            user.SetBudgetTotalCurrency(Currency.EUR);

            Assert.Equal(Currency.EUR, user.BudgetTotalCurrency);
        }

        [Fact]
        public void SetBudgetTotalCurrency_NullAfterACurrency_ClearsIt()
        {
            AppUser user = new();
            user.SetBudgetTotalCurrency(Currency.EUR);

            user.SetBudgetTotalCurrency(null);

            Assert.Null(user.BudgetTotalCurrency);
        }
    }
}
