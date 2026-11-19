using Kvit.Contracts.Persistence;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;

namespace Kvit.Application.Persistence
{
    public static class UnitOfWorkExtensions
    {
        public static async Task<TResult> RunInTransactionAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, CancellationToken cancellationToken)
            where TResult : Result
        {
            await unitOfWork.OpenTransactionAsync(cancellationToken);
            TResult result = await work();
            if (!result.IsSuccess)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return result;
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return result;
        }

        public static Task<TResult> RunInTransactionOnceMoreIfClientRequestIdWasTakenAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, CancellationToken cancellationToken)
            where TResult : Result
        {
            return unitOfWork.RunInTransactionOnceMoreIfAsync(work, exception => exception is ClientRequestIdAlreadySavedException, cancellationToken);
        }

        public static Task<TResult> RunInTransactionOnceMoreIfRecurringIncomeEntryWasSavedOrDeadlockedAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, CancellationToken cancellationToken)
            where TResult : Result
        {
            return unitOfWork.RunInTransactionOnceMoreIfAsync(work, exception => exception is RecurringIncomeEntryAlreadySavedException or DeadlockDetectedException, cancellationToken);
        }

        public static async Task<TResult> RunInTransactionOrRefuseIfCategoryNameWasTakenAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, Func<Result, TResult> refusal, CancellationToken cancellationToken)
            where TResult : Result
        {
            try
            {
                return await unitOfWork.RunInTransactionAsync(work, cancellationToken);
            }
            catch (CategoryNameAlreadySavedException)
            {
                return refusal(Category.NameTakenMeanwhile());
            }
        }

        public static Task<TResult> RunInTransactionOnceMoreIfMemberChangedMeanwhileAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, CancellationToken cancellationToken)
            where TResult : Result
        {
            return unitOfWork.RunInTransactionOnceMoreIfAsync(work, exception => exception is MemberChangedMeanwhileException, cancellationToken);
        }

        public static Task<TResult> RunInTransactionOnceMoreIfBudgetLimitChangedMeanwhileAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, CancellationToken cancellationToken)
            where TResult : Result
        {
            return unitOfWork.RunInTransactionOnceMoreIfAsync(work, exception => exception is BudgetLimitChangedMeanwhileException, cancellationToken);
        }

        public static Task<TResult> RunInTransactionOnceMoreIfSettlementChangedMeanwhileAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, CancellationToken cancellationToken)
            where TResult : Result
        {
            return unitOfWork.RunInTransactionOnceMoreIfAsync(work, exception => exception is SettlementChangedMeanwhileException, cancellationToken);
        }

        public static Task<TResult> RunInTransactionOnceMoreIfMergeWasSavedAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, CancellationToken cancellationToken)
            where TResult : Result
        {
            return unitOfWork.RunInTransactionOnceMoreIfAsync(work, exception => exception is MergeAlreadySavedException, cancellationToken);
        }

        public static Task<TResult> RunInTransactionOnceMoreIfAccountChangedMeanwhileAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, CancellationToken cancellationToken)
            where TResult : Result
        {
            return unitOfWork.RunInTransactionOnceMoreIfAsync(work, exception => exception is AccountChangedMeanwhileException, cancellationToken);
        }

        public static Task<TResult> RunInTransactionKeepingRefusalsOnceMoreIfAccountChangedMeanwhileAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, CancellationToken cancellationToken)
            where TResult : Result
        {
            return OnceMoreIfAsync(() => unitOfWork.RunInTransactionKeepingRefusalsAsync(work, cancellationToken), exception => exception is AccountChangedMeanwhileException);
        }

        public static async Task<TResult> RunInTransactionOrAnswerIfExpenseChangedMeanwhileAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, Func<Task<TResult>> answerAfterChange, CancellationToken cancellationToken)
            where TResult : Result
        {
            try
            {
                return await unitOfWork.RunInTransactionAsync(work, cancellationToken);
            }
            catch (Exception exception) when (exception is ExpenseChangedMeanwhileException or DeadlockDetectedException)
            {
                return await unitOfWork.RunInTransactionAsync(answerAfterChange, cancellationToken);
            }
        }

        private static Task<TResult> RunInTransactionOnceMoreIfAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, Func<Exception, bool> isWorthOneMoreRun, CancellationToken cancellationToken)
            where TResult : Result
        {
            return OnceMoreIfAsync(() => unitOfWork.RunInTransactionAsync(work, cancellationToken), isWorthOneMoreRun);
        }

        private static async Task<TResult> RunInTransactionKeepingRefusalsAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, CancellationToken cancellationToken)
            where TResult : Result
        {
            await unitOfWork.OpenTransactionAsync(cancellationToken);
            TResult result = await work();
            await unitOfWork.CommitAsync(cancellationToken);

            return result;
        }

        private static async Task<TResult> OnceMoreIfAsync<TResult>(Func<Task<TResult>> run, Func<Exception, bool> isWorthOneMoreRun)
        {
            try
            {
                return await run();
            }
            catch (Exception exception) when (isWorthOneMoreRun(exception))
            {
                return await run();
            }
        }
    }
}
