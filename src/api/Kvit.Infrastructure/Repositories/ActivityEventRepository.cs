using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Infrastructure.Persistence;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class ActivityEventRepository(AppDbContext _context) : IActivityEventRepository
    {
        public void Add(ActivityEvent activityEvent)
        {
            _context.ActivityEvents.Add(activityEvent);
        }
    }
}
