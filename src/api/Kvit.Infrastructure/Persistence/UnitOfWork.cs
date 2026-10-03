using Kvit.Contracts.Persistence;

namespace Kvit.Infrastructure.Persistence
{
    public sealed class UnitOfWork(AppDbContext _context) : IUnitOfWork
    {
        public async Task OpenTransactionAsync(CancellationToken cancellationToken)
        {
            await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Database.CommitTransactionAsync(cancellationToken);
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            return _context.Database.RollbackTransactionAsync(cancellationToken);
        }
    }
}
