using Kvit.Contracts.Persistence;
using Kvit.Domain.Entities;
using Kvit.Infrastructure.Auth;
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
            catch (DbUpdateConcurrencyException exception) when (exception.Entries.Any(entry => entry.Entity is BudgetLimit))
            {
                await DiscardAsync(context, cancellationToken);
                throw new BudgetLimitChangedMeanwhileException(exception);
            }
            catch (DbUpdateConcurrencyException exception) when (exception.Entries.Any(entry => entry.Entity is Expense or ExpenseShare))
            {
                await DiscardAsync(context, cancellationToken);
                throw new ExpenseChangedMeanwhileException(exception);
            }
            catch (DbUpdateConcurrencyException exception) when (exception.Entries.Any(entry => entry.Entity is Settlement))
            {
                await DiscardAsync(context, cancellationToken);
                throw new SettlementChangedMeanwhileException(exception);
            }
            catch (DbUpdateConcurrencyException exception) when (exception.Entries.Any(entry => entry.Entity is AppUser))
            {
                await DiscardAsync(context, cancellationToken);
                throw new AccountChangedMeanwhileException(exception);
            }
            catch (DbUpdateException exception) when (IsUniqueViolationOf(exception, ExpenseShareConfiguration.PrimaryKeyName))
            {
                await DiscardAsync(context, cancellationToken);
                throw new ExpenseChangedMeanwhileException(exception);
            }
            catch (DbUpdateException exception) when (IsUniqueViolationOf(exception, BudgetLimitConfiguration.UserCategoryMonthIndexName))
            {
                await DiscardAsync(context, cancellationToken);
                throw new BudgetLimitChangedMeanwhileException(exception);
            }
            catch (DbUpdateException exception) when (IsUniqueViolationOf(exception, ExpenseConfiguration.ClientRequestIdIndexName, SettlementConfiguration.ClientRequestIdIndexName, PersonalEntryConfiguration.ClientRequestIdIndexName, RecurringIncomeConfiguration.ClientRequestIdIndexName))
            {
                await DiscardAsync(context, cancellationToken);
                throw new ClientRequestIdAlreadySavedException(exception);
            }
            catch (DbUpdateException exception) when (IsUniqueViolationOf(exception, PersonalEntryConfiguration.RecurringIncomeDateIndexName))
            {
                await DiscardAsync(context, cancellationToken);
                throw new RecurringIncomeEntryAlreadySavedException(exception);
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
            catch (InvalidOperationException exception) when (IsDeadlock(exception))
            {
                await DiscardAsync(context, cancellationToken);
                throw new DeadlockDetectedException(exception);
            }
        }

        public static async Task ExecuteSqlOrReportMergeAlreadySavedAsync(this AppDbContext context, FormattableString sql, CancellationToken cancellationToken)
        {
            try
            {
                await context.Database.ExecuteSqlInterpolatedAsync(sql, cancellationToken);
            }
            catch (PostgresException exception) when (IsUniqueViolationOf(exception, DuplicateAnswerConfiguration.OneMergePerEntryIndexName, DuplicateAnswerConfiguration.OneMergePerExpenseIndexName))
            {
                await DiscardAsync(context, cancellationToken);
                throw new MergeAlreadySavedException(exception);
            }
        }

        public static async Task<AccountChangedMeanwhileException> DiscardBecauseAccountChangedMeanwhileAsync(this AppDbContext context, string identityFailure, CancellationToken cancellationToken)
        {
            await DiscardAsync(context, cancellationToken);

            return new AccountChangedMeanwhileException(identityFailure);
        }

        private static bool IsDeadlock(InvalidOperationException exception)
        {
            return exception.InnerException is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected } };
        }

        private static bool IsUniqueViolationOf(DbUpdateException exception, params string[] indexNames)
        {
            return exception.InnerException is PostgresException violation && IsUniqueViolationOf(violation, indexNames);
        }

        private static bool IsUniqueViolationOf(PostgresException exception, params string[] indexNames)
        {
            return exception.SqlState == PostgresErrorCodes.UniqueViolation && indexNames.Contains(exception.ConstraintName);
        }

        private static async Task DiscardAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            await context.Database.RollbackTransactionAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }
    }
}
