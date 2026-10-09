namespace Kvit.Api.Settings
{
    public static class NbrmSetting
    {
        private const string BaseUrlKey = "ExchangeRates:NbrmBaseUrl";

        public static Uri ReadBaseUrl(IConfiguration configuration)
        {
            string? baseUrl = configuration[BaseUrlKey];
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? address))
            {
                throw new InvalidOperationException(
                    $"The setting {BaseUrlKey} is missing or not an absolute address ('{baseUrl}'), so Kvit cannot ask NBRM for the euro rate. " +
                    $"Put the NBRM service address under {BaseUrlKey} in src/api/Kvit.Api/appsettings.json, " +
                    "or on a server set the environment variable ExchangeRates__NbrmBaseUrl.");
            }

            return address;
        }
    }
}
