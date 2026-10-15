using System.Globalization;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public static class ClosingRows
    {
        public static Task<string?> StatusAsync(MigratedDatabase database, Guid groupId)
        {
            return GroupRows.GroupValueAsync(database, groupId, "status");
        }

        public static Task<string?> ClosingStartedAtUtcAsync(MigratedDatabase database, Guid groupId)
        {
            return GroupRows.GroupValueAsync(database, groupId, "closing_started_at AT TIME ZONE 'UTC'");
        }

        public static Task<string?> FinishedAtUtcAsync(MigratedDatabase database, Guid groupId)
        {
            return GroupRows.GroupValueAsync(database, groupId, "finished_at AT TIME ZONE 'UTC'");
        }

        public static async Task<int> CountUsageEventsOfAsync(MigratedDatabase database, params Guid[] userIds)
        {
            List<string?> counts = await database.QueryAsync(
                "SELECT count(*)::text FROM usage_events WHERE user_id = ANY(@user_ids)",
                new NpgsqlParameter("user_ids", userIds));

            return int.Parse(Assert.Single(counts)!, CultureInfo.InvariantCulture);
        }
    }
}
