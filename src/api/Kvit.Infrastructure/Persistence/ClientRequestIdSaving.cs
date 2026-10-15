using Kvit.Contracts.Persistence;
using Kvit.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kvit.Infrastructure.Persistence
{
    public static class ClientRequestIdSaving
    {
        public static async Task SaveChangesOrReportTakenClientRequestIdAsync(this AppDbContext context, CancellationToken cancellationToken)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsClientRequestIdUniqueViolation(exception))
            {
                await context.Database.RollbackTransactionAsync(cancellationToken);
                context.ChangeTracker.Clear();
                throw new ClientRequestIdAlreadySavedException(exception);
            }
        }

        private static bool IsClientRequestIdUniqueViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: ExpenseConfiguration.ClientRequestIdIndexName or SettlementConfiguration.ClientRequestIdIndexName,
            };
        }
    }
}
