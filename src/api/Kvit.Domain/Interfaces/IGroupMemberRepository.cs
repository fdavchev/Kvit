using Kvit.Domain.Entities;

namespace Kvit.Domain.Interfaces
{
    public interface IGroupMemberRepository
    {
        void Add(GroupMember member);

        Task<GroupRoster> RosterOfAsync(Guid groupId, CancellationToken cancellationToken);

        Task SaveAsync(CancellationToken cancellationToken);
    }
}
