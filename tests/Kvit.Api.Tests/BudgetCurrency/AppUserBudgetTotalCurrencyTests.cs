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
    }
}
