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
    }
}
