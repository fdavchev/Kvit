using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakeGroupRepository : IGroupRepository
    {
        private readonly Dictionary<Guid, Group> _groups = [];
        private readonly HashSet<(Guid GroupId, Guid UserId)> _currentMembers = [];

        public List<Group> Added { get; } = [];

        public void Seed(Group group, params Guid[] currentMemberUserIds)
        {
            _groups[group.Id] = group;
            foreach (Guid userId in currentMemberUserIds)
            {
                _currentMembers.Add((group.Id, userId));
            }
        }

        public void Add(Group group)
        {
            Added.Add(group);
        }

        public Task<Result<Group>> FindForMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken)
        {
            Result<Group> found = FindIncludingDeleted(groupId, userId);
            if (found.IsSuccess && found.Value.DeletedAt is not null)
            {
                return Task.FromResult(Group.NotFound<Group>(groupId));
            }

            return Task.FromResult(found);
        }

        public Task<Result<Group>> FindIncludingDeletedForMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(FindIncludingDeleted(groupId, userId));
        }

        public Task<Result<Group>> FindByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken)
        {
            Group? group = _groups.Values.SingleOrDefault(candidate => candidate.InviteToken == inviteToken && candidate.DeletedAt is null);
            if (group is null)
            {
                return Task.FromResult(Group.InviteNotFound<Group>());
            }

            return Task.FromResult(Result.Ok(group));
        }

        private Result<Group> FindIncludingDeleted(Guid groupId, Guid userId)
        {
            if (!_groups.TryGetValue(groupId, out Group? group) || !_currentMembers.Contains((groupId, userId)))
            {
                return Group.NotFound<Group>(groupId);
            }

            return Result.Ok(group);
        }
    }
}
