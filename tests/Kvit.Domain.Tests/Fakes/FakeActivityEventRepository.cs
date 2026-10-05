using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakeActivityEventRepository : IActivityEventRepository
    {
        public List<ActivityEvent> Added { get; } = [];

        public void Add(ActivityEvent activityEvent)
        {
            Added.Add(activityEvent);
        }
    }
}
