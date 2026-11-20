using Kvit.Contracts.Persistence;

namespace Kvit.Infrastructure.Auth
{
    internal static class AccountWriteRetry
    {
        public static async Task<T> OnceMoreIfAccountChangedMeanwhileAsync<T>(Func<Task<T>> write)
        {
            try
            {
                return await write();
            }
            catch (AccountChangedMeanwhileException)
            {
                return await write();
            }
        }
    }
}
