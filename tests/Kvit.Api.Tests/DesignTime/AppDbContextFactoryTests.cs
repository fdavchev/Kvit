using Kvit.Api.Persistence;
using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Postgres;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.DesignTime
{
    public class AppDbContextFactoryTests(PostgresFixture _postgres)
    {
        [Fact]
        public void Create_WithOnlyTheConnectionString_ReturnsAContext()
        {
            using AppDbContext context = AppDbContextFactory.Create(DesignTimeConfiguration.WithConnectionString(KvitApiFactory.UnreachableConnectionString));

            Assert.NotNull(context);
        }

        [Fact]
        public void Create_WithoutAConnectionString_ThrowsAnInvalidOperationExceptionNamingTheSetting()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => AppDbContextFactory.Create(DesignTimeConfiguration.Empty()));

            Assert.Contains("ConnectionStrings:KvitDatabase", exception.Message);
        }

        [Fact]
        public void Create_BuildsTheSnakeCaseNamedTables()
        {
            using AppDbContext context = AppDbContextFactory.Create(DesignTimeConfiguration.WithConnectionString(KvitApiFactory.UnreachableConnectionString));

            List<string> model = ModelDescription.Of(context);

            Assert.Contains(model, line => line.StartsWith("users: ") && line.Contains("must_change_password"));
            Assert.Contains(model, line => line.StartsWith("data_protection_keys: ") && line.Contains("friendly_name"));
        }

        [Fact]
        public void Create_BuildsTheSameModelAsTheRunningApp()
        {
            using KvitApiFactory app = KvitApiFactory.WithConnectionString(KvitApiFactory.UnreachableConnectionString);
            using IServiceScope scope = app.Services.CreateScope();
            AppDbContext appContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            using AppDbContext factoryContext = AppDbContextFactory.Create(DesignTimeConfiguration.WithConnectionString(KvitApiFactory.UnreachableConnectionString));

            List<string> factoryModel = ModelDescription.Of(factoryContext);

            Assert.Equal(ModelDescription.Of(appContext), factoryModel);
        }

        [Fact]
        public void Create_BuildsAModelWithNoChangesMissingFromTheMigrations()
        {
            using AppDbContext context = AppDbContextFactory.Create(DesignTimeConfiguration.WithConnectionString(_postgres.CreateDatabase()));

            bool hasPendingModelChanges = context.Database.HasPendingModelChanges();

            Assert.False(hasPendingModelChanges);
        }
    }
}
