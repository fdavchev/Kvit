using Kvit.Api.Tests.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Accounts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public static class AccountPurges
    {
        public static async Task PurgeAsync(MigratedDatabase database, Guid userId, string nameChoice, DateTimeOffset now)
        {
            using IServiceScope scope = database.CreateScope();
            IAccountPurger purger = scope.ServiceProvider.GetRequiredService<IAccountPurger>();

            await purger.PurgeAccountAsync(userId, Enum.Parse<DeletionNameChoice>(nameChoice), now, TestContext.Current.CancellationToken);
        }

        public static async Task<int> PurgeDueAsync(MigratedDatabase database, DateTimeOffset now)
        {
            using IServiceScope scope = database.CreateScope();
            IAccountPurger purger = scope.ServiceProvider.GetRequiredService<IAccountPurger>();

            return await purger.PurgeDueAccountsAsync(now, TestContext.Current.CancellationToken);
        }

        public static IAccountPurger ResolveFrom(IServiceScope scope)
        {
            return scope.ServiceProvider.GetRequiredService<IAccountPurger>();
        }
    }
}
