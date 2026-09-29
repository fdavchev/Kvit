using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Postgres;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class MigrationTests(PostgresFixture _postgres)
    {
        [Fact]
        public async Task Migrate_OnAnEmptyDatabase_AppliesEveryMigration()
        {
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(_postgres.CreateDatabase());
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
            Assert.Empty(await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Migrate_CalledTwice_ChangesNothingTheSecondTime()
        {
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(_postgres.CreateDatabase());
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
            List<string> appliedAfterFirstRun = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.Equal(appliedAfterFirstRun, await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Model_HasNoChangesMissingFromTheMigrations()
        {
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(_postgres.CreateDatabase());
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            bool hasPendingModelChanges = context.Database.HasPendingModelChanges();

            Assert.False(hasPendingModelChanges);
        }
    }
}
