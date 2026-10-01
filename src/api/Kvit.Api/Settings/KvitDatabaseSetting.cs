namespace Kvit.Api.Settings
{
    public static class KvitDatabaseSetting
    {
        private const string ConnectionStringName = "KvitDatabase";

        public static string Read(IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString(ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"The setting ConnectionStrings:{ConnectionStringName} is missing or empty, so Kvit cannot reach its PostgreSQL database. " +
                    $"For local work, store it with: dotnet user-secrets set \"ConnectionStrings:{ConnectionStringName}\" \"<connection string>\" --project src/api/Kvit.Api " +
                    $"(see docs/guides/phase-04-local-setup.md). On a server, set the environment variable ConnectionStrings__{ConnectionStringName}.");
            }

            return connectionString;
        }
    }
}
