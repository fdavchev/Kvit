using Microsoft.Extensions.Configuration;

namespace Kvit.Api.Tests.DesignTime
{
    public static class DesignTimeConfiguration
    {
        public static IConfiguration WithConnectionString(string connectionString)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:KvitDatabase", connectionString)])
                .Build();
        }

        public static IConfiguration Empty()
        {
            return new ConfigurationBuilder().Build();
        }
    }
}
