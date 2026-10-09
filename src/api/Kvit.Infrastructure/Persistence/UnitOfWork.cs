using Kvit.Contracts.Persistence;
using Kvit.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsClientRequestIdUniqueViolation(exception))
            {
                await _context.Database.RollbackTransactionAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                throw new ClientRequestIdAlreadySavedException(exception);
            }

            await _context.Database.CommitTransactionAsync(cancellationToken);
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            return _context.Database.RollbackTransactionAsync(cancellationToken);
        }

        private static bool IsClientRequestIdUniqueViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: ExpenseConfiguration.ClientRequestIdIndexName,
            };
        }
    }
}
