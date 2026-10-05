using Kvit.Domain.Entities;

namespace Kvit.Domain.Interfaces
{
    public interface IActivityEventRepository
    {
        void Add(ActivityEvent activityEvent);
    }
}
