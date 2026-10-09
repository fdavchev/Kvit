using Kvit.Contracts.Persistence;
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

        public static async Task<TResult> RunInTransactionOnceMoreIfClientRequestIdWasTakenAsync<TResult>(this IUnitOfWork unitOfWork, Func<Task<TResult>> work, CancellationToken cancellationToken)
            where TResult : Result
        {
            try
            {
                return await unitOfWork.RunInTransactionAsync(work, cancellationToken);
            }
            catch (ClientRequestIdAlreadySavedException)
            {
                return await unitOfWork.RunInTransactionAsync(work, cancellationToken);
            }
        }
    }
}
