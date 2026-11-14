using Kvit.Application.Dispatching;
using Kvit.Contracts.OwnerTools;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.OwnerTools
{
    public sealed class GetOwnerToolsGroupQueryHandler(
        AppDbContext _context,
        TimeProvider _timeProvider) : IQueryHandler<GetOwnerToolsGroupQuery, OwnerToolsGroupResponse>
    {
        public async Task<Result<OwnerToolsGroupResponse>> Handle(GetOwnerToolsGroupQuery query, CancellationToken cancellationToken)
        {
            OwnerToolsGroupResponse? group = await OwnerToolsGroupResponseOf(query.GroupId, _timeProvider.GetUtcNow()).SingleOrDefaultAsync(cancellationToken);
            if (group is null)
            {
                return Group.NotFoundAnywhere<OwnerToolsGroupResponse>(query.GroupId);
            }

            return Result.Ok(group);
        }

        private IQueryable<OwnerToolsGroupResponse> OwnerToolsGroupResponseOf(Guid groupId, DateTimeOffset now)
        {
            IQueryable<GroupMember> currentMembers = _context.CurrentGroupMembers();

            return _context.Groups
                .AsNoTracking()
                .Where(group => group.Id == groupId)
                .Select(group => new OwnerToolsGroupResponse(
                    group.Id,
                    group.Name,
                    group.Emoji,
                    Group.EffectiveStatusOf(group.Status, group.ClosingStartedAt, now),
                    currentMembers.Count(member => member.GroupId == group.Id),
                    _context.Users.Where(user => user.Id == group.OwnerUserId).Select(user => user.DisplayName).First(),
                    group.CreatedAt,
                    group.DeletedAt != null,
                    group.AdminClosedAt));
        }
    }
}
