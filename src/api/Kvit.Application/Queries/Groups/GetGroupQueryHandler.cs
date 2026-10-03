using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Groups;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Groups
{
    public sealed class GetGroupQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetGroupQuery, GroupResponse>
    {
        public async Task<Result<GroupResponse>> Handle(GetGroupQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<GroupResponse>(userId.Error, userId.ErrorCode);
            }

            GroupResponse? group = await GroupResponseOf(query.GroupId, userId.Value).SingleOrDefaultAsync(cancellationToken);
            if (group is null)
            {
                return Group.NotFound<GroupResponse>(query.GroupId);
            }

            return Result.Ok(group);
        }

        private IQueryable<GroupResponse> GroupResponseOf(Guid groupId, Guid userId)
        {
            IQueryable<GroupMember> currentMembers = _context.CurrentGroupMembers();

            return _context.GroupsWithCurrentMember(userId)
                .AsNoTracking()
                .Where(group => group.Id == groupId && group.DeletedAt == null)
                .Select(group => new GroupResponse(
                    group.Id,
                    group.Kind,
                    group.Name,
                    group.Emoji,
                    group.DefaultCurrency,
                    group.Status,
                    group.OwnerUserId,
                    group.OwnerUserId == userId,
                    currentMembers.Count(member => member.GroupId == group.Id),
                    group.InviteToken));
        }
    }
}
