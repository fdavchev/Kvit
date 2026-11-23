using Kvit.Domain.Accounts;

namespace Kvit.Infrastructure.Persistence
{
    public static class AdminQueries
    {
        public static IQueryable<Guid> AdminUserIds(this AppDbContext context)
        {
            return context.UserRoles
                .Where(userRole => context.Roles.Any(role => role.Id == userRole.RoleId && role.Name == Roles.Admin))
                .Select(userRole => userRole.UserId);
        }
    }
}
