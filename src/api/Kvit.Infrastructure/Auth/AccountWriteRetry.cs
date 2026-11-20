using Kvit.Contracts.Persistence;

namespace Kvit.Infrastructure.Auth
{
    internal static class AccountWriteRetry
    {
        public static async Task<T> OnceMoreIfAccountChangedMeanwhileAsync<T>(IUnitOfWork unitOfWork, Func<Task<T>> write)
        {
            try
            {
                return await write();
            }
            catch (AccountChangedMeanwhileException exception)
            {
                unitOfWork.LogCaughtClash(exception, "the account write runs once more");
                return await write();
            }
        }
    }
}
