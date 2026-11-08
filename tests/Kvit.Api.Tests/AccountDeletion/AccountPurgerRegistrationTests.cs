using Kvit.Api.Tests.Expenses;
using Kvit.Contracts.Auth;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class AccountPurgerRegistrationTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public void Purger_ResolvedFromAScope_IsAvailable()
        {
            using IServiceScope scope = _app.CreateScope();

            IAccountPurger purger = AccountPurges.ResolveFrom(scope);

            Assert.NotNull(purger);
        }

        [Fact]
        public void Purger_ResolvedTwiceInOneScope_IsTheSameInstance()
        {
            using IServiceScope scope = _app.CreateScope();

            IAccountPurger first = AccountPurges.ResolveFrom(scope);
            IAccountPurger second = AccountPurges.ResolveFrom(scope);

            Assert.Same(first, second);
        }

        [Fact]
        public void Purger_ResolvedFromTwoScopes_IsADifferentInstanceInEach()
        {
            using IServiceScope firstScope = _app.CreateScope();
            using IServiceScope secondScope = _app.CreateScope();

            IAccountPurger first = AccountPurges.ResolveFrom(firstScope);
            IAccountPurger second = AccountPurges.ResolveFrom(secondScope);

            Assert.NotSame(first, second);
        }
    }
}
