using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public static class BudgetLimitRows
    {
        public static readonly Guid BuiltInFoodId = Guid.Parse("c47e9a10-5f3b-4d2e-9a61-000000000001");

        public static async Task<Guid> InsertAsync(
            MigratedDatabase database,
            Guid userId,
            Guid? categoryId = null,
            DateOnly? effectiveFrom = null,
            long? limitMinor = 800_000)
        {
            Guid id = Guid.CreateVersion7();
            await database.ExecuteAsync(
                "INSERT INTO budget_limits (id, user_id, category_id, effective_from, limit_minor, created_at) "
                + "VALUES (@id, @user_id, @category_id, @effective_from, @limit_minor, now())",
                new NpgsqlParameter("id", id),
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("category_id", NpgsqlDbType.Uuid) { Value = (object?)categoryId ?? DBNull.Value },
                new NpgsqlParameter("effective_from", NpgsqlDbType.Date) { Value = effectiveFrom ?? new DateOnly(2026, 10, 1) },
                new NpgsqlParameter("limit_minor", NpgsqlDbType.Bigint) { Value = (object?)limitMinor ?? DBNull.Value });

            return id;
        }

        public static async Task<int> CountOfUserAsync(MigratedDatabase database, Guid userId)
        {
            List<string?> counts = await database.QueryAsync(
                "SELECT count(*)::text FROM budget_limits WHERE user_id = @user_id",
                new NpgsqlParameter("user_id", userId));

            return int.Parse(Assert.Single(counts)!);
        }
    }
}
