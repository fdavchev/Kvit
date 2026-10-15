using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.UsageEvents
{
    public sealed class RecordActive(IUsageEventRepository _usageEvents)
    {
        public async Task<Result> Execute(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            if (await _usageEvents.IsActiveOnAsync(userId, UsageEvent.UtcDayOf(now), cancellationToken))
            {
                return Result.Ok();
            }

            _usageEvents.Add(UsageEvent.Active(userId, now));

            return Result.Ok();
        }
    }
}
