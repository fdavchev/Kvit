using Kvit.Domain.Entities;
using Kvit.Domain.Results;

namespace Kvit.Domain.Interfaces
{
    public interface IGroupRepository
    {
        void Add(Group group);

        Task<Result<Group>> FindForMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken);

        Task<Result<Group>> FindIncludingDeletedForMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken);

        Task<Result<Group>> FindByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken);
    }
}
