using Kvit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

        public static IQueryable<Guid?> OwnerOfGroupForMember(this AppDbContext context, Guid groupId, Guid userId)
        {
            return context.GroupsWithCurrentMember(userId)
                .AsNoTracking()
                .Where(group => group.Id == groupId && group.DeletedAt == null)
                .Select(group => (Guid?)group.OwnerUserId);
        }

        public static IQueryable<NamedMember> NamedGroupMembers(this AppDbContext context, Guid groupId)
        {
            return context.GroupMembers
                .Where(member => member.GroupId == groupId)
                .LeftJoin(
                    context.Users,
                    member => member.UserId,
                    user => (Guid?)user.Id,
                    (member, user) => new NamedMember
                    {
                        Member = member,
                        DisplayName = user == null ? member.Name : user.DisplayName,
                    });
        }

        public static IQueryable<NamedMember> InJoiningOrder(this IQueryable<NamedMember> members)
        {
            return members
                .OrderBy(named => named.Member.JoinedAt)
                .ThenBy(named => named.Member.Id);
        }
    }
}
