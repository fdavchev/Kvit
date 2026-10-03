using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public static class RejectedInsert
    {
        public static async Task AssertAsync(Func<Task> insert, string expectedSqlState)
        {
            PostgresException error = await Assert.ThrowsAsync<PostgresException>(insert);

            Assert.Equal(expectedSqlState, error.SqlState);
        }
    }
}
