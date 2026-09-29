using Kvit.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class DataProtectionKeysTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Theory]
        [InlineData("id", "integer")]
        [InlineData("friendly_name", "text")]
        [InlineData("xml", "text")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "data_protection_keys", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Fact]
        public async Task Key_SavedThroughTheContext_ReadsBackUnchanged()
        {
            const string friendlyName = "key-2026-09-29";
            const string xml = "<key id=\"6f1c\" version=\"1\"><creationDate>2026-09-29T10:00:00Z</creationDate></key>";
            int keyId;
            using (IServiceScope writeScope = _database.CreateScope())
            {
                AppDbContext writeContext = writeScope.ServiceProvider.GetRequiredService<AppDbContext>();
                DataProtectionKey key = new() { FriendlyName = friendlyName, Xml = xml };
                writeContext.DataProtectionKeys.Add(key);
                await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                keyId = key.Id;
            }

            using IServiceScope readScope = _database.CreateScope();
            AppDbContext readContext = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
            DataProtectionKey stored = await readContext.DataProtectionKeys.SingleAsync(row => row.Id == keyId, TestContext.Current.CancellationToken);

            Assert.Equal(friendlyName, stored.FriendlyName);
            Assert.Equal(xml, stored.Xml);
        }

        [Fact]
        public async Task Keys_SavedWithoutIds_GetDistinctGeneratedIds()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DataProtectionKey first = new() { FriendlyName = "first", Xml = "<key/>" };
            DataProtectionKey second = new() { FriendlyName = "second", Xml = "<key/>" };
            context.DataProtectionKeys.AddRange(first, second);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.True(first.Id > 0);
            Assert.NotEqual(first.Id, second.Id);
        }
    }
}
