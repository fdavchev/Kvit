using Kvit.Domain.Entities;

namespace Kvit.Domain.Interfaces
{
    public interface IUsageEventRepository
    {
        void Add(UsageEvent usageEvent);

        Task<bool> IsActiveOnAsync(Guid userId, DateOnly day, CancellationToken cancellationToken);
    }
}
