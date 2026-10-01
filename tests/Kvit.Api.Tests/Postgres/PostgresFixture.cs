using Kvit.Api.Tests.Postgres;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(PostgresFixture))]

namespace Kvit.Api.Tests.Postgres
{
    public class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();

        public ValueTask InitializeAsync()
        {
            return new ValueTask(_container.StartAsync());
        }

        public ValueTask DisposeAsync()
        {
            return _container.DisposeAsync();
        }

        public string CreateDatabase()
        {
            string databaseName = $"kvit_test_{Guid.NewGuid():N}";

            using NpgsqlConnection connection = new(_container.GetConnectionString());
            connection.Open();
            using NpgsqlCommand command = new($"CREATE DATABASE {databaseName}", connection);
            command.ExecuteNonQuery();

            NpgsqlConnectionStringBuilder connectionString = new(_container.GetConnectionString()) { Database = databaseName };
            return connectionString.ConnectionString;
        }
    }
}
