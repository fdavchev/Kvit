using System.Globalization;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Terms
{
    public static class TermsRows
    {
        public static Task SetAsync(MigratedDatabase database, Guid userId, string? version, DateTimeOffset? acceptedAt)
        {
            return database.ExecuteAsync(
                "UPDATE users SET terms_version = @version, terms_accepted_at = @accepted_at WHERE id = @id",
                new NpgsqlParameter("id", userId),
                new NpgsqlParameter("version", NpgsqlDbType.Text) { Value = (object?)version ?? DBNull.Value },
                new NpgsqlParameter("accepted_at", NpgsqlDbType.TimestampTz) { Value = (object?)acceptedAt ?? DBNull.Value });
        }

        public static Task ClearAsync(MigratedDatabase database, Guid userId)
        {
            return SetAsync(database, userId, null, null);
        }

        public static async Task<StoredTerms> ReadAsync(MigratedDatabase database, Guid userId)
        {
            List<string?> versions = await database.QueryAsync(
                "SELECT terms_version FROM users WHERE id = @id",
                new NpgsqlParameter("id", userId));
            List<string?> acceptedAts = await database.QueryAsync(
                "SELECT to_char(terms_accepted_at AT TIME ZONE 'UTC', 'YYYY-MM-DD\"T\"HH24:MI:SS.US\"Z\"') FROM users WHERE id = @id",
                new NpgsqlParameter("id", userId));

            string? acceptedAt = Assert.Single(acceptedAts);
            return new StoredTerms(
                Assert.Single(versions),
                acceptedAt is null ? null : DateTimeOffset.Parse(acceptedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
        }
    }
}
