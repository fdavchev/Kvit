using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakeGroupMemberRepository : IGroupMemberRepository
    {
        public List<GroupMember> Added { get; } = [];

        public void Add(GroupMember member)
        {
            Added.Add(member);
        }
    }
}
