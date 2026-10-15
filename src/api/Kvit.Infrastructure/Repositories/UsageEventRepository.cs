using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class UsageEventRepository(AppDbContext _context) : IUsageEventRepository
    {
        public void Add(UsageEvent usageEvent)
        {
            _context.UsageEvents.Add(usageEvent);
        }

        public Task<bool> IsActiveOnAsync(Guid userId, DateOnly day, CancellationToken cancellationToken)
        {
            return _context.UsageEvents.AnyAsync(
                usageEvent => usageEvent.UserId == userId && usageEvent.Type == UsageEventType.Active && usageEvent.OccurredOn == day,
                cancellationToken);
        }
    }
}
