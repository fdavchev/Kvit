using Kvit.Domain.Results;

namespace Kvit.Contracts.Auth
{
    public interface ICurrentUserProvider
    {
        Result<Guid> GetCurrentUserId();
    }
}
