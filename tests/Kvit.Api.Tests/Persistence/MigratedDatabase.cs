using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Postgres;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class MigratedDatabase : IAsyncLifetime
    {
        private readonly string _connectionString;
        protected readonly KvitApiFactory _factory;

        public MigratedDatabase(PostgresFixture postgres)
        {
            _connectionString = postgres.CreateDatabase();
            _factory = KvitApiFactory.WithConnectionString(_connectionString);
        }

        public async ValueTask InitializeAsync()
        {
            using IServiceScope scope = CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        public virtual ValueTask DisposeAsync()
        {
            return _factory.DisposeAsync();
        }

        public KvitApiFactory CreateAnotherInstance()
        {
            return _factory.CreateAnotherInstanceOnTheSameDatabase();
        }

        public IServiceScope CreateScope()
        {
            return _factory.Services.CreateScope();
        }

        public async Task<List<string?>> QueryAsync(string sql, params NpgsqlParameter[] parameters)
        {
            await using NpgsqlConnection connection = new(_connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(sql, connection);
            command.Parameters.AddRange(parameters);

            List<string?> firstColumn = [];
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            {
                firstColumn.Add(reader.IsDBNull(0) ? null : reader.GetString(0));
            }

            return firstColumn;
        }

        public async Task ExecuteAsync(string sql, params NpgsqlParameter[] parameters)
        {
            await using NpgsqlConnection connection = new(_connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(sql, connection);
            command.Parameters.AddRange(parameters);

            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
    }
}
