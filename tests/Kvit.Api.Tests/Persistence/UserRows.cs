using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public static class UserRows
    {
        public static async Task<Guid> AddAsync(MigratedDatabase database)
        {
            using IServiceScope scope = database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser user = TestUsers.Create();
            context.Users.Add(user);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return user.Id;
        }
    }
}
