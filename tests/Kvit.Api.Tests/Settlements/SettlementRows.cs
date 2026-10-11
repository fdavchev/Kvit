using System.Globalization;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public static class SettlementRows
    {
        public static async Task<string?> ValueAsync(MigratedDatabase database, Guid settlementId, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM settlements WHERE id = @id",
                new NpgsqlParameter("id", settlementId));

            return Assert.Single(values);
        }

        public static Task<int> CountAsync(MigratedDatabase database, Guid groupId)
        {
            return CountScalarAsync(
                database,
                "SELECT count(*)::text FROM settlements WHERE group_id = @group_id",
                new NpgsqlParameter("group_id", groupId));
        }

        public static Task<int> CountEventsAsync(MigratedDatabase database, Guid settlementId, string type)
        {
            return CountScalarAsync(
                database,
                "SELECT count(*)::text FROM activity_events WHERE settlement_id = @settlement_id AND type = @type",
                new NpgsqlParameter("settlement_id", settlementId),
                new NpgsqlParameter("type", type));
        }

        public static async Task<string?> EventValueAsync(MigratedDatabase database, Guid settlementId, string type, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM activity_events WHERE settlement_id = @settlement_id AND type = @type",
                new NpgsqlParameter("settlement_id", settlementId),
                new NpgsqlParameter("type", type));

            return Assert.Single(values);
        }

        public static Task<int> CountConfirmedUsageEventsAsync(MigratedDatabase database, Guid userId)
        {
            return GroupRows.CountUsageEventsAsync(database, userId, "SettlementConfirmed");
        }

        public static async Task<string> SnapshotAsync(MigratedDatabase database, Guid groupId)
        {
            List<string?> parts = await database.QueryAsync(
                "SELECT part FROM ("
                + "SELECT 1 AS position, COALESCE(string_agg(s::text, '|' ORDER BY s.id), '') AS part FROM settlements s WHERE s.group_id = @group_id "
                + "UNION ALL SELECT 2, COALESCE(string_agg(a::text, '|' ORDER BY a.id), '') FROM activity_events a WHERE a.group_id = @group_id "
                + "UNION ALL SELECT 3, COALESCE(string_agg(u::text, '|' ORDER BY u.id), '') FROM usage_events u WHERE u.type = 'SettlementConfirmed'"
                + ") parts ORDER BY position",
                new NpgsqlParameter("group_id", groupId));

            return string.Join('#', parts);
        }

        public static string UtcTextOf(DateTimeOffset moment)
        {
            return moment.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static async Task<int> CountScalarAsync(MigratedDatabase database, string sql, params NpgsqlParameter[] parameters)
        {
            List<string?> counts = await database.QueryAsync(sql, parameters);

            return int.Parse(Assert.Single(counts)!, CultureInfo.InvariantCulture);
        }
    }
}
