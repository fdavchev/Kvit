using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public sealed class InsertGate : IAsyncDisposable
    {
        private const long LockKey = 8_000_001;
        private const string AnyRow = "true";
        private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(25);

        private readonly MigratedDatabase _database;
        private readonly string _table;
        private readonly NpgsqlConnection _holder;

        private InsertGate(MigratedDatabase database, string table, NpgsqlConnection holder)
        {
            _database = database;
            _table = table;
            _holder = holder;
        }

        public static async Task<InsertGate> CloseAsync(MigratedDatabase database, string table, string rowCondition = AnyRow)
        {
            await database.ExecuteAsync(
                $"CREATE OR REPLACE FUNCTION {GateNameOf(table)}() RETURNS trigger LANGUAGE plpgsql AS $$ "
                + $"BEGIN PERFORM pg_advisory_xact_lock({LockKey}); RETURN NEW; END $$; "
                + $"DROP TRIGGER IF EXISTS {GateNameOf(table)} ON {table}; "
                + $"CREATE TRIGGER {GateNameOf(table)} BEFORE INSERT ON {table} FOR EACH ROW WHEN ({rowCondition}) EXECUTE FUNCTION {GateNameOf(table)}();");

            NpgsqlConnection holder = new(ConnectionStringOf(database));
            await holder.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand lockCommand = new($"SELECT pg_advisory_lock({LockKey})", holder);
            await lockCommand.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

            return new InsertGate(database, table, holder);
        }

        public async Task WaitUntilRequestsAreHeldAsync(int expectedHeldRequests)
        {
            DateTime giveUpAt = DateTime.UtcNow + WaitLimit;
            int held = 0;
            while (DateTime.UtcNow < giveUpAt)
            {
                List<string?> counts = await _database.QueryAsync(
                    "SELECT count(*)::text FROM pg_locks WHERE locktype = 'advisory' AND NOT granted "
                    + "AND database = (SELECT oid FROM pg_database WHERE datname = current_database())");
                held = int.Parse(Assert.Single(counts)!);
                if (held >= expectedHeldRequests)
                {
                    return;
                }

                await Task.Delay(PollInterval, TestContext.Current.CancellationToken);
            }

            throw new TimeoutException($"Expected {expectedHeldRequests} requests to be held at the {_table} insert, but only {held} arrived within {WaitLimit.TotalSeconds} seconds.");
        }

        public async Task OpenAsync()
        {
            await using NpgsqlCommand unlockCommand = new($"SELECT pg_advisory_unlock({LockKey})", _holder);
            await unlockCommand.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await OpenAsync();
            await _holder.DisposeAsync();
            await _database.ExecuteAsync($"DROP TRIGGER IF EXISTS {GateNameOf(_table)} ON {_table}; DROP FUNCTION IF EXISTS {GateNameOf(_table)}();");
        }

        private static string GateNameOf(string table)
        {
            return $"wait_at_{table}_gate";
        }

        private static string ConnectionStringOf(MigratedDatabase database)
        {
            using IServiceScope scope = database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return context.Database.GetConnectionString()
                ?? throw new InvalidOperationException("The test database has no connection string.");
        }
    }
}
