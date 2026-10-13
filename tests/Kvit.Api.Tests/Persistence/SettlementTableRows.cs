using System.Globalization;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public static class SettlementTableRows
    {
        public const long DefaultAmountMinor = 50_000;

        public static async Task<Guid> InsertSettlementAsync(
            MigratedDatabase database,
            Guid groupId,
            Guid fromMemberId,
            Guid toMemberId,
            Guid recordedByUserId,
            long amountMinor = DefaultAmountMinor,
            string currency = "MKD",
            string status = "Pending",
            Guid? clientRequestId = null,
            DateTimeOffset? deletedAt = null)
        {
            Guid settlementId = Guid.CreateVersion7();
            object? deletedBy = deletedAt is null ? null : recordedByUserId;

            await database.ExecuteAsync(
                "INSERT INTO settlements (id, group_id, from_member_id, to_member_id, amount_minor, currency, status, recorded_by_user_id, created_at, "
                + "deleted_at, deleted_by_user_id, client_request_id) "
                + "VALUES (@id, @group_id, @from_member_id, @to_member_id, @amount_minor, @currency, @status, @recorded_by, now(), "
                + "@deleted_at, @deleted_by, @client_request_id)",
                new NpgsqlParameter("id", settlementId),
                new NpgsqlParameter("group_id", groupId),
                new NpgsqlParameter("from_member_id", fromMemberId),
                new NpgsqlParameter("to_member_id", toMemberId),
                new NpgsqlParameter("amount_minor", amountMinor),
                new NpgsqlParameter("currency", currency),
                new NpgsqlParameter("status", status),
                new NpgsqlParameter("recorded_by", recordedByUserId),
                Nullable("deleted_at", NpgsqlDbType.TimestampTz, deletedAt),
                Nullable("deleted_by", NpgsqlDbType.Uuid, deletedBy),
                new NpgsqlParameter("client_request_id", clientRequestId ?? Guid.CreateVersion7()));

            return settlementId;
        }

        public static async Task<int> CountSettlementsAsync(MigratedDatabase database, Guid groupId)
        {
            List<string?> counts = await database.QueryAsync(
                "SELECT count(*)::text FROM settlements WHERE group_id = @group_id",
                new NpgsqlParameter("group_id", groupId));

            return int.Parse(Assert.Single(counts)!, CultureInfo.InvariantCulture);
        }

        public static async Task<string?> SettlementValueAsync(MigratedDatabase database, Guid settlementId, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM settlements WHERE id = @id",
                new NpgsqlParameter("id", settlementId));

            return Assert.Single(values);
        }

        private static NpgsqlParameter Nullable(string name, NpgsqlDbType type, object? value)
        {
            return new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value };
        }
    }
}
