using Kvit.Domain.Entities;

namespace Kvit.Infrastructure.Persistence
{
    public static class GroupQueries
    {
        public static IQueryable<GroupMember> CurrentGroupMembers(this AppDbContext context)
        {
            return context.GroupMembers.Where(member => member.RemovedAt == null);
        }

        public static IQueryable<Group> GroupsWithCurrentMember(this AppDbContext context, Guid userId)
        {
            IQueryable<GroupMember> currentMembers = context.CurrentGroupMembers();

            return context.Groups.Where(group => currentMembers.Any(member => member.GroupId == group.Id && member.UserId == userId));
        }
    }
}
