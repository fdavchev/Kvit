using System.Globalization;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public static class ClosingResponseRows
    {
        public static Task InsertAsync(
            MigratedDatabase database,
            Guid groupId,
            Guid memberId,
            string response = "Confirmed",
            string? reason = null,
            DateTimeOffset? respondedAt = null)
        {
            return database.ExecuteAsync(
                "INSERT INTO closing_responses (group_id, member_id, response, reason, responded_at) "
                + "VALUES (@group_id, @member_id, @response, @reason, COALESCE(@responded_at, now()))",
                new NpgsqlParameter("group_id", groupId),
                new NpgsqlParameter("member_id", memberId),
                new NpgsqlParameter("response", response),
                Nullable("reason", NpgsqlDbType.Text, reason),
                Nullable("responded_at", NpgsqlDbType.TimestampTz, respondedAt));
        }

        public static async Task<int> CountAsync(MigratedDatabase database, Guid groupId)
        {
            List<string?> counts = await database.QueryAsync(
                "SELECT count(*)::text FROM closing_responses WHERE group_id = @group_id",
                new NpgsqlParameter("group_id", groupId));

            return int.Parse(Assert.Single(counts)!, CultureInfo.InvariantCulture);
        }

        public static async Task<string?> ValueAsync(MigratedDatabase database, Guid groupId, Guid memberId, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM closing_responses WHERE group_id = @group_id AND member_id = @member_id",
                new NpgsqlParameter("group_id", groupId),
                new NpgsqlParameter("member_id", memberId));

            return Assert.Single(values);
        }

        private static NpgsqlParameter Nullable(string name, NpgsqlDbType type, object? value)
        {
            return new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value };
        }
    }
}
