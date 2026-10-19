using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class LetGuestInAgain(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IActivityEventRepository _activityEvents,
        CancelClosingOnChange _cancelClosingOnChange,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, Guid memberId, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForOwnerAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            Result changeable = await _cancelClosingOnChange.Execute(found.Value, userId, cancellationToken);
            if (!changeable.IsSuccess)
            {
                return changeable;
            }

            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            Result<NamedMember> target = roster.FindCurrent(memberId);
            if (!target.IsSuccess)
            {
                return target;
            }

            if (!target.Value.IsGuest)
            {
                return Result.Failure($"Member {memberId} is not a guest, so there is no guest to let in again.", ResultCodes.MEMBER_NOT_GUEST);
            }

            target.Value.Member.TurnIntoPlainName();
            _activityEvents.Add(ActivityEvent.GuestLetInAgain(groupId, userId, memberId, target.Value.DisplayName, _timeProvider.GetUtcNow()));

            return Result.Ok();
        }
    }
}
