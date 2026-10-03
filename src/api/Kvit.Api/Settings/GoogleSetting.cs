namespace Kvit.Api.Settings
{
    public static class GoogleSetting
    {
        private const string ClientIdKey = "Google:ClientId";

        public static string Read(IConfiguration configuration)
        {
            string? clientId = configuration[ClientIdKey];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException(
                    $"The setting {ClientIdKey} is missing or empty, so Kvit cannot check Google sign-in tokens. " +
                    $"Put the OAuth client ID from the Google Cloud console under {ClientIdKey} in src/api/Kvit.Api/appsettings.json " +
                    "(a client ID is public, not a secret; see docs/guides/phase-06-google.md), " +
                    "or on a server set the environment variable Google__ClientId.");
            }

            return clientId;
        }
    }
}
