using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Infrastructure.Persistence;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class UsageEventRepository(AppDbContext _context) : IUsageEventRepository
    {
        public void Add(UsageEvent usageEvent)
        {
            _context.UsageEvents.Add(usageEvent);
        }
    }
}
