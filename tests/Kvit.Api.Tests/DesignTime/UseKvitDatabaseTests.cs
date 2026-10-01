using Kvit.Api.Tests.Hosting;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kvit.Api.Tests.DesignTime
{
    public class UseKvitDatabaseTests
    {
        [Fact]
        public void UseKvitDatabase_AddsTheNpgsqlProviderAndTheSnakeCaseNamingConvention()
        {
            DbContextOptionsBuilder options = new();

            options.UseKvitDatabase(KvitApiFactory.UnreachableConnectionString);

            List<string> extensionNames = [.. options.Options.Extensions.Select(extension => extension.GetType().Name)];
            Assert.Contains("NpgsqlOptionsExtension", extensionNames);
            Assert.Contains("NamingConventionsOptionsExtension", extensionNames);
        }
    }
}
