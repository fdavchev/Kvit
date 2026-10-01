using Kvit.Api.Settings;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kvit.Api.Persistence
{
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddUserSecrets<AppDbContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            return Create(configuration);
        }

        public static AppDbContext Create(IConfiguration configuration)
        {
            DbContextOptionsBuilder<AppDbContext> options = new();
            options.UseKvitDatabase(KvitDatabaseSetting.Read(configuration));

            return new AppDbContext(options.Options);
        }
    }
}
