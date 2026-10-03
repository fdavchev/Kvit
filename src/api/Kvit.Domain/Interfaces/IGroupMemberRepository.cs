using Kvit.Domain.Entities;

namespace Kvit.Domain.Interfaces
{
    public interface IGroupMemberRepository
    {
        void Add(GroupMember member);
    }
}
