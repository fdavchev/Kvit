using Kvit.Contracts.Persistence;
using Kvit.Domain.Entities;
using Kvit.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kvit.Infrastructure.Persistence
{
    public static class DuplicateSaving
    {
        public static async Task SaveChangesOrReportDuplicateAsync(this AppDbContext context, CancellationToken cancellationToken)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception) when (exception.Entries.Any(entry => entry.Entity is GroupMember))
            {
                await DiscardAsync(context, cancellationToken);
                throw new MemberChangedMeanwhileException(exception);
            }
            catch (DbUpdateException exception) when (IsUniqueViolationOf(exception, ExpenseConfiguration.ClientRequestIdIndexName, SettlementConfiguration.ClientRequestIdIndexName, PersonalEntryConfiguration.ClientRequestIdIndexName))
            {
                await DiscardAsync(context, cancellationToken);
                throw new ClientRequestIdAlreadySavedException(exception);
            }
            catch (DbUpdateException exception) when (IsUniqueViolationOf(exception, UsageEventConfiguration.ActiveDayIndexName))
            {
                await DiscardAsync(context, cancellationToken);
                throw new ActiveDayAlreadySavedException(exception);
            }
            catch (DbUpdateException exception) when (IsUniqueViolationOf(exception, CategoryConfiguration.ActiveNameIndexName))
            {
                await DiscardAsync(context, cancellationToken);
                throw new CategoryNameAlreadySavedException(exception);
            }
        }

        private static bool IsUniqueViolationOf(DbUpdateException exception, params string[] indexNames)
        {
            return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } violation
                && indexNames.Contains(violation.ConstraintName);
        }

        private static async Task DiscardAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            await context.Database.RollbackTransactionAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }
    }
}
