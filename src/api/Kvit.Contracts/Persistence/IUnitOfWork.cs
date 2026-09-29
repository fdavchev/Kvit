namespace Kvit.Contracts.Persistence
{
    public interface IUnitOfWork
    {
        Task OpenTransactionAsync(CancellationToken cancellationToken);

        Task CommitAsync(CancellationToken cancellationToken);

        Task RollbackAsync(CancellationToken cancellationToken);
    }
}
