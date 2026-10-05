using Kvit.Domain.Entities;
using Kvit.Domain.Results;

namespace Kvit.Domain.Interfaces
{
    public static class GroupRepositoryExtensions
    {
        public static async Task<Result<Group>> FindForOwnerAsync(this IGroupRepository groups, Guid groupId, Guid userId, CancellationToken cancellationToken)
        {
            Result<Group> found = await groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            Result owner = found.Value.CheckOwner(userId);
            if (!owner.IsSuccess)
            {
                return owner.ToFailure<Group>();
            }

            return found;
        }
    }
}
