using Kvit.Contracts.Persistence;
using Microsoft.Extensions.Logging;

namespace Kvit.Infrastructure.Persistence
{
    public sealed class UnitOfWork(AppDbContext _context, ILogger<UnitOfWork> _logger) : IUnitOfWork
    {
        public async Task OpenTransactionAsync(CancellationToken cancellationToken)
        {
            await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesOrReportDuplicateAsync(cancellationToken);
            await _context.Database.CommitTransactionAsync(cancellationToken);
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            return _context.Database.RollbackTransactionAsync(cancellationToken);
        }

        public void LogCaughtClash(Exception clash, string nextStep)
        {
            _logger.LogWarning(clash, "A same-moment clash with another request was caught ({ClashType}); {NextStep}.", clash.GetType().Name, nextStep);
        }
    }
}
