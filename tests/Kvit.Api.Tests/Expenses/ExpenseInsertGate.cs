using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public sealed class ExpenseInsertGate : IAsyncDisposable
    {
        private const long LockKey = 8_000_001;
        private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(25);

        private readonly ExpensesApp _app;
        private readonly NpgsqlConnection _holder;

        private ExpenseInsertGate(ExpensesApp app, NpgsqlConnection holder)
        {
            _app = app;
            _holder = holder;
        }

        public static async Task<ExpenseInsertGate> CloseAsync(ExpensesApp app)
        {
            await app.ExecuteAsync(
                "CREATE OR REPLACE FUNCTION wait_at_expense_gate() RETURNS trigger LANGUAGE plpgsql AS $$ "
                + $"BEGIN PERFORM pg_advisory_xact_lock({LockKey}); RETURN NEW; END $$; "
                + "DROP TRIGGER IF EXISTS wait_at_expense_gate ON expenses; "
                + "CREATE TRIGGER wait_at_expense_gate BEFORE INSERT ON expenses FOR EACH ROW EXECUTE FUNCTION wait_at_expense_gate();");

            NpgsqlConnection holder = new(ConnectionStringOf(app));
            await holder.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand lockCommand = new($"SELECT pg_advisory_lock({LockKey})", holder);
            await lockCommand.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

            return new ExpenseInsertGate(app, holder);
        }

        public async Task WaitUntilRequestsAreHeldAsync(int expectedHeldRequests)
        {
            DateTime giveUpAt = DateTime.UtcNow + WaitLimit;
            int held = 0;
            while (DateTime.UtcNow < giveUpAt)
            {
                List<string?> counts = await _app.QueryAsync(
                    "SELECT count(*)::text FROM pg_locks WHERE locktype = 'advisory' AND NOT granted "
                    + "AND database = (SELECT oid FROM pg_database WHERE datname = current_database())");
                held = int.Parse(Assert.Single(counts)!);
                if (held >= expectedHeldRequests)
                {
                    return;
                }

                await Task.Delay(PollInterval, TestContext.Current.CancellationToken);
            }

            throw new TimeoutException($"Expected {expectedHeldRequests} requests to be held at the expense insert, but only {held} arrived within {WaitLimit.TotalSeconds} seconds.");
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
            await _app.ExecuteAsync("DROP TRIGGER IF EXISTS wait_at_expense_gate ON expenses; DROP FUNCTION IF EXISTS wait_at_expense_gate();");
        }

        private static string ConnectionStringOf(ExpensesApp app)
        {
            using IServiceScope scope = app.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return context.Database.GetConnectionString()
                ?? throw new InvalidOperationException("The test database has no connection string.");
        }
    }
}
