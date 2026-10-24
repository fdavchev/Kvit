using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public sealed class DeadlockPartner : IAsyncDisposable
    {
        private const string Savepoint = "before_the_second_entry";
        private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(25);

        private readonly MigratedDatabase _database;
        private readonly NpgsqlConnection _connection;
        private readonly NpgsqlTransaction _transaction;

        private DeadlockPartner(MigratedDatabase database, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            _database = database;
            _connection = connection;
            _transaction = transaction;
        }

        public static async Task<DeadlockPartner> BeginAsync(MigratedDatabase database)
        {
            NpgsqlConnection connection = await database.OpenConnectionAsync();
            NpgsqlTransaction transaction = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
            DeadlockPartner partner = new(database, connection, transaction);
            await partner.ExecuteAsync("SET LOCAL deadlock_timeout = '1min'");

            return partner;
        }

        public Task InsertEntryAsync(Guid userId, Guid recurringIncomeId, DateOnly entryDate)
        {
            return ExecuteAsync(
                "INSERT INTO personal_entries (id, user_id, kind, amount_minor, currency, mkd_per_eur, rate_date, entry_date, category_id, note, recurring_income_id, client_request_id, created_at) "
                + "VALUES (@id, @user_id, 'Income', 4500000, 'MKD', 61.5, DATE '2026-10-05', @entry_date, NULL, NULL, @recurring_income_id, NULL, now())",
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("entry_date", NpgsqlDbType.Date) { Value = entryDate },
                new NpgsqlParameter("recurring_income_id", recurringIncomeId));
        }

        public Task SetSavepointAsync()
        {
            return _transaction.SaveAsync(Savepoint, TestContext.Current.CancellationToken);
        }

        public Task RollBackToSavepointAsync()
        {
            return _transaction.RollbackAsync(Savepoint, TestContext.Current.CancellationToken);
        }

        public Task RollBackAsync()
        {
            return _transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }

        public async Task WaitUntilARequestWaitsForItAsync()
        {
            DateTime giveUpAt = DateTime.UtcNow + WaitLimit;
            while (DateTime.UtcNow < giveUpAt)
            {
                List<string?> counts = await _database.QueryAsync(
                    "SELECT count(*)::text FROM pg_locks waiting JOIN pg_stat_activity activity ON activity.pid = waiting.pid "
                    + "WHERE waiting.locktype = 'transactionid' AND NOT waiting.granted "
                    + "AND activity.datname = current_database() AND waiting.pid <> @partner_pid",
                    new NpgsqlParameter("partner_pid", _connection.ProcessID));
                if (int.Parse(Assert.Single(counts)!) >= 1)
                {
                    return;
                }

                await Task.Delay(PollInterval, TestContext.Current.CancellationToken);
            }

            throw new TimeoutException($"No request started waiting for the partner's uncommitted entry within {WaitLimit.TotalSeconds} seconds.");
        }

        public async ValueTask DisposeAsync()
        {
            await _transaction.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private async Task ExecuteAsync(string sql, params NpgsqlParameter[] parameters)
        {
            await using NpgsqlCommand command = new(sql, _connection, _transaction);
            command.Parameters.AddRange(parameters);

            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
    }
}
