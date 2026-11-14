namespace Kvit.Api.Settings
{
    public static class CleanupSetting
    {
        private const string SecretKey = "Cleanup:Secret";

        public static string? ReadSecret(IConfiguration configuration)
        {
            string? secret = configuration[SecretKey];
            return string.IsNullOrWhiteSpace(secret) ? null : secret;
        }
    }
}
