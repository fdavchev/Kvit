using Kvit.Domain.Results;

namespace Kvit.Contracts.OwnerTools
{
    public interface IAccountModeration
    {
        Task<Result> SuspendAsync(Guid adminUserId, Guid personId, string reason, CancellationToken cancellationToken);

        Task<Result> UnsuspendAsync(Guid adminUserId, Guid personId, string reason, CancellationToken cancellationToken);

        Task<Result> EraseNameAsync(Guid adminUserId, Guid personId, string reason, CancellationToken cancellationToken);
    }
}
