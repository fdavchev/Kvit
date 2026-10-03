using Kvit.Domain.Results;

namespace Kvit.Contracts.Auth
{
    public interface IGoogleTokenChecker
    {
        Task<Result<GoogleIdentity>> CheckAsync(string idToken, CancellationToken cancellationToken);
    }
}
