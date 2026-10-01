using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public static class PostgresErrors
    {
        public static async Task<PostgresException> SaveChangesFailingAsync(AppDbContext context)
        {
            DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync(TestContext.Current.CancellationToken));

            return Assert.IsType<PostgresException>(exception.InnerException);
        }
    }
}
