using Kvit.Application.Dispatching;
using Kvit.Contracts.Activity;
using Kvit.Contracts.Auth;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Activity
{
    public sealed class GetActivityQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetActivityQuery, ActivityResponse>
    {
        public const int FeedLimit = 200;

        public async Task<Result<ActivityResponse>> Handle(GetActivityQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<ActivityResponse>(userId.Error, userId.ErrorCode);
            }

            Guid? ownerUserId = await _context.OwnerOfGroupForMember(query.GroupId, userId.Value).SingleOrDefaultAsync(cancellationToken);
            if (ownerUserId is null)
            {
                return Group.NotFound<ActivityResponse>(query.GroupId);
            }

            List<ActivityEventDto> events = await ActivityEventDtosOf(query.GroupId).ToListAsync(cancellationToken);
            List<ActivityEventRow> rows = [.. events.Select(activityEvent => new ActivityEventRow(
                activityEvent.Id,
                activityEvent.Type,
                activityEvent.ActorUserId,
                activityEvent.ActorName,
                activityEvent.ExpenseId,
                activityEvent.MemberId,
                ActivityJson.Parse(activityEvent.Changes),
                ActivityJson.Parse(activityEvent.Data),
                activityEvent.CreatedAt))];

            return Result.Ok(new ActivityResponse(rows));
        }

        private IQueryable<ActivityEventDto> ActivityEventDtosOf(Guid groupId)
        {
            return _context.ActivityEvents
                .AsNoTracking()
                .Where(activityEvent => activityEvent.GroupId == groupId)
                .Join(_context.Users, activityEvent => activityEvent.ActorUserId, user => user.Id, (activityEvent, user) => new { Event = activityEvent, ActorName = user.DisplayName })
                .OrderByDescending(row => row.Event.CreatedAt)
                .ThenByDescending(row => row.Event.Id)
                .Take(FeedLimit)
                .Select(row => new ActivityEventDto(
                    row.Event.Id,
                    row.Event.Type,
                    row.Event.ActorUserId,
                    row.ActorName,
                    row.Event.ExpenseId,
                    row.Event.MemberId,
                    row.Event.Changes,
                    row.Event.Data,
                    row.Event.CreatedAt));
        }
    }
}
