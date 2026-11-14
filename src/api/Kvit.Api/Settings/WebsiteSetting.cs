namespace Kvit.Api.Settings
{
    public static class WebsiteSetting
    {
        private const string OriginKey = "Website:Origin";

        public static Uri ReadOrigin(IConfiguration configuration)
        {
            string? origin = configuration[OriginKey];
            if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? address) || !IsAllowedScheme(address) || !IsOriginOnly(address))
            {
                throw new InvalidOperationException(
                    $"The setting {OriginKey} is missing or not a website origin ('{origin}'), so the wake page cannot send people back to Kvit. " +
                    $"It must look like https://kvit-mk.pages.dev (http only for localhost), with no path, query or fragment. " +
                    $"Put it under {OriginKey} in src/api/Kvit.Api/appsettings.json, or on a server set the environment variable Website__Origin.");
            }

            return address;
        }

        private static bool IsAllowedScheme(Uri address)
        {
            return address.Scheme == Uri.UriSchemeHttps || (address.Scheme == Uri.UriSchemeHttp && address.IsLoopback);
        }

        private static bool IsOriginOnly(Uri address)
        {
            return address.AbsolutePath == "/" && address.Query.Length == 0 && address.Fragment.Length == 0 && address.UserInfo.Length == 0;
        }
    }
}
