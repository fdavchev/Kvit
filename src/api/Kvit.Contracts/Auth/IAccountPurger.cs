using Kvit.Domain.Accounts;

namespace Kvit.Contracts.Auth
{
    public interface IAccountPurger
    {
        Task<bool> PurgeAccountAsync(Guid userId, DeletionNameChoice nameChoice, DateTimeOffset now, CancellationToken cancellationToken);

        Task<int> PurgeDueAccountsAsync(DateTimeOffset now, CancellationToken cancellationToken);
    }
}
