using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public static class GroupPlanRows
    {
        public const string NoPlan = "none";

        public static Task SetAsync(MigratedDatabase database, Guid groupId, long? amountMinor, string? currency)
        {
            return database.ExecuteAsync(
                "UPDATE groups SET plan_amount_minor = @amount, plan_currency = @currency WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("amount", NpgsqlDbType.Bigint) { Value = (object?)amountMinor ?? DBNull.Value },
                new NpgsqlParameter("currency", NpgsqlDbType.Text) { Value = (object?)currency ?? DBNull.Value });
        }

        public static async Task<string> PlanOfAsync(MigratedDatabase database, Guid groupId)
        {
            List<string?> values = await database.QueryAsync(
                "SELECT CASE WHEN plan_amount_minor IS NULL AND plan_currency IS NULL THEN @none "
                + "ELSE COALESCE(plan_amount_minor::text, 'null') || ':' || COALESCE(plan_currency, 'null') END "
                + "FROM groups WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("none", NoPlan));

            return Assert.Single(values)!;
        }

        public static string TextOf(long amountMinor, string currency)
        {
            return $"{amountMinor}:{currency}";
        }
    }
}
