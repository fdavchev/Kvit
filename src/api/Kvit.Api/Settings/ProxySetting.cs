namespace Kvit.Api.Settings
{
    public static class ProxySetting
    {
        private const string SharedSecretKey = "Proxy:SharedSecret";

        public static string? Read(IConfiguration configuration, IHostEnvironment environment)
        {
            string? sharedSecret = configuration[SharedSecretKey];
            if (!string.IsNullOrWhiteSpace(sharedSecret))
            {
                return sharedSecret;
            }

            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    $"The setting {SharedSecretKey} is missing or empty, so Kvit cannot tell requests that came through its Cloudflare proxy " +
                    "from requests sent straight to the API, and anyone could dodge the log-in and sign-up limits. " +
                    "On the server, set the environment variable Proxy__SharedSecret to the same long random value as API_PROXY_SECRET " +
                    "in Cloudflare (see docs/guides/free-hosting-setup.md).");
            }

            return null;
        }
    }
}
