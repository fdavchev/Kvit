using Kvit.Contracts.Persistence;

namespace Kvit.Api.Tests.PersonalEntries
{
    public sealed class ChangingUnitOfWork(IUnitOfWork _inner, ChangeAfterNextTransactionOpens _change) : IUnitOfWork
    {
        public async Task OpenTransactionAsync(CancellationToken cancellationToken)
        {
            await _inner.OpenTransactionAsync(cancellationToken);
            await _change.RunIfArmedAsync();
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            return _inner.CommitAsync(cancellationToken);
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            return _inner.RollbackAsync(cancellationToken);
        }
    }
}
