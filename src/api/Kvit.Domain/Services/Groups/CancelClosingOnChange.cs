using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class CancelClosingOnChange(
        IClosingResponseRepository _closingResponses,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Group group, Guid userId, CancellationToken cancellationToken)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result changeable = group.CheckCanBeChangedBy(userId, now);
            if (!changeable.IsSuccess)
            {
                return changeable;
            }

            if (group.Status != GroupStatus.Closing)
            {
                return Result.Ok();
            }

            await _closingResponses.RemoveAllOfAsync(group.Id, cancellationToken);
            group.CancelClosing();
            _activityEvents.Add(ActivityEvent.ClosingCancelled(group.Id, userId, now));

            return Result.Ok();
        }
    }
}
