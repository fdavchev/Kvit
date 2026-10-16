using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakeUsageEventRepository : IUsageEventRepository
    {
        public List<UsageEvent> Added { get; } = [];

        public void Add(UsageEvent usageEvent)
        {
            Added.Add(usageEvent);
        }

        public Task<bool> IsActiveOnAsync(Guid userId, DateOnly day, CancellationToken cancellationToken)
        {
            return Task.FromResult(Added.Any(usageEvent => usageEvent.UserId == userId && usageEvent.Type == UsageEventType.Active && usageEvent.OccurredOn == day));
        }
    }
}
