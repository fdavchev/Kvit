using Kvit.Api.Tests.Persistence;
using Npgsql;

namespace Kvit.Api.Tests.Dashboard
{
    public static class ActiveRows
    {
        public static Task<int> CountOfAsync(MigratedDatabase database, Guid userId)
        {
            return GroupRows.CountUsageEventsAsync(database, userId, "Active");
        }

        public static async Task<int> CountOfEveryoneAsync(MigratedDatabase database)
        {
            List<string?> counts = await database.QueryAsync("SELECT count(*)::text FROM usage_events WHERE type = 'Active'");

            return int.Parse(counts.Single()!);
        }

        public static async Task<List<string>> DaysOfAsync(MigratedDatabase database, Guid userId)
        {
            List<string?> days = await database.QueryAsync(
                "SELECT occurred_on::text FROM usage_events WHERE user_id = @user_id AND type = 'Active' ORDER BY occurred_on",
                new NpgsqlParameter("user_id", userId));

            return [.. days.Select(day => day!)];
        }

        public static async Task<string?> ValueOfAsync(MigratedDatabase database, Guid userId, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM usage_events WHERE user_id = @user_id AND type = 'Active'",
                new NpgsqlParameter("user_id", userId));

            return values.Single();
        }
    }
}
