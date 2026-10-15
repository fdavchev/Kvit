using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class ConfirmClosing(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IClosingResponseRepository _closingResponses,
        IActivityEventRepository _activityEvents,
        FinishExpiredClosing _finishExpiredClosing,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, CancellationToken cancellationToken)
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

            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            NamedMember ownRow = roster.CurrentRowOf(userId)
                ?? throw new InvalidOperationException($"User {userId} was found as a current member of group {groupId}, but has no current row in it.");
            IReadOnlyList<ClosingResponse> responses = await _closingResponses.ResponsesOfAsync(groupId, cancellationToken);
            if (responses.Any(response => response.MemberId == ownRow.Member.Id))
            {
                return Result.Failure($"Member {ownRow.Member.Id} already answered the closing of group {groupId}.", ResultCodes.CLOSING_ALREADY_ANSWERED);
            }

            _closingResponses.Add(ClosingResponse.Confirmed(groupId, ownRow.Member.Id, now));
            _activityEvents.Add(ActivityEvent.ClosingConfirmed(groupId, userId, now));

            HashSet<Guid> confirmedMemberIds = [.. responses.Where(response => response.Response == ClosingAnswer.Confirmed).Select(response => response.MemberId), ownRow.Member.Id];
            if (roster.ClosingConfirmers(group.OwnerUserId).All(row => confirmedMemberIds.Contains(row.Member.Id)))
            {
                group.Finish(now);
                _activityEvents.Add(ActivityEvent.GroupFinished(groupId, userId, now));
            }

            return Result.Ok();
        }
    }
}
