using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class ObjectToClosing(
        IGroupRepository _groups,
        IClosingResponseRepository _closingResponses,
        IActivityEventRepository _activityEvents,
        FinishExpiredClosing _finishExpiredClosing,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, string? reason, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            Group group = found.Value;
            DateTimeOffset now = _timeProvider.GetUtcNow();
            _finishExpiredClosing.Execute(group, now);
            Result canAnswer = group.CheckCanAnswerClosing(userId, now);
            if (!canAnswer.IsSuccess)
            {
                return canAnswer;
            }

            Result<string?> objectionReason = ClosingResponse.ObjectionReasonOf(reason);
            if (!objectionReason.IsSuccess)
            {
                return objectionReason;
            }

            await _closingResponses.RemoveAllOfAsync(groupId, cancellationToken);
            group.CancelClosing();
            _activityEvents.Add(ActivityEvent.ClosingObjected(groupId, userId, objectionReason.Value, now));

            return Result.Ok();
        }
    }
}
