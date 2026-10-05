using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class CreateGroup(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IActivityEventRepository _activityEvents,
        IUsageEventRepository _usageEvents,
        IInviteTokenGenerator _inviteTokenGenerator,
        TimeProvider _timeProvider)
    {
        public Result<Guid> Execute(Guid ownerUserId, string ownerAccountName, string name, string emoji, string currency)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result<Group> created = Group.Create(name, emoji, currency, ownerUserId, _inviteTokenGenerator.NewToken(), now);
            if (!created.IsSuccess)
            {
                return Result.Failure<Guid>(created.Error, created.ErrorCode);
            }

            Group group = created.Value;
            _groups.Add(group);
            _members.Add(GroupMember.ForAccount(group.Id, ownerUserId, ownerAccountName, ownerUserId, now));
            _activityEvents.Add(ActivityEvent.GroupCreated(group.Id, ownerUserId, group.Name, now));
            _usageEvents.Add(UsageEvent.GroupCreated(ownerUserId, now));

            return Result.Ok(group.Id);
        }
    }
}
