using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakeGroupMemberRepository : IGroupMemberRepository
    {
        private readonly List<NamedMember> _rows = [];

        public List<GroupMember> Added { get; } = [];

        public List<IReadOnlyList<MemberSnapshot>> SavedStates { get; } = [];

        public void Seed(GroupMember member)
        {
            Seed(member, member.Name);
        }

        public void Seed(GroupMember member, string displayName)
        {
            _rows.Add(new NamedMember { Member = member, DisplayName = displayName });
        }

        public void Add(GroupMember member)
        {
            Added.Add(member);
            Seed(member);
        }

        public Task<GroupRoster> RosterOfAsync(Guid groupId, CancellationToken cancellationToken)
        {
            List<NamedMember> rows = [.. _rows.Where(row => row.Member.GroupId == groupId)];

            return Task.FromResult(new GroupRoster(rows));
        }

        public Task SaveAsync(CancellationToken cancellationToken)
        {
            SavedStates.Add([.. _rows.Select(row => MemberSnapshot.Of(row.Member))]);

            return Task.CompletedTask;
        }
    }
}
