using Kvit.Domain.Entities;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Activity
{
    public static class ActivityEventQueries
    {
        public static IQueryable<ActivityEventDto> NewestActivityEventDtosOf(this AppDbContext context, IQueryable<ActivityEvent> activityEvents)
        {
            return activityEvents
                .AsNoTracking()
                .Join(context.Users, activityEvent => activityEvent.ActorUserId, user => user.Id, (activityEvent, user) => new { Event = activityEvent, ActorName = user.DisplayName })
                .OrderByDescending(row => row.Event.CreatedAt)
                .ThenByDescending(row => row.Event.Id)
                .Select(row => new ActivityEventDto(
                    row.Event.Id,
                    row.Event.GroupId,
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
