using System.Net;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Postgres;
using Kvit.Domain.Entities;
using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class AuthApp : MigratedDatabase
    {
        private readonly WebApplicationFactory<Program> _withTestControllers;

        public AuthApp(PostgresFixture postgres)
            : base(postgres)
        {
            _withTestControllers = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.AddControllers().AddApplicationPart(typeof(UnprotectedTestController).Assembly)));
        }

        public HttpClient CreateClient()
        {
            return _factory.CreateClient(HttpsWithCookies());
        }

        public HttpClient CreateClientWithoutCookies()
        {
            return _factory.CreateClient(HttpsWithoutCookies());
        }

        public HttpClient CreateClientWithTestControllers()
        {
            return _withTestControllers.CreateClient(HttpsWithCookies());
        }

        public async Task<Guid> CreateAccountAsync(RegistrationForm form)
        {
            HttpClient client = CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
        }

        public async Task<AppUser> FindUserAsync(string email)
        {
            using IServiceScope scope = CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await context.Users.AsNoTracking().SingleAsync(user => user.Email == email, TestContext.Current.CancellationToken);
        }

        public async Task<int> CountUsersAsync()
        {
            using IServiceScope scope = CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await context.Users.CountAsync(TestContext.Current.CancellationToken);
        }

        public async Task<int> CountUsersWithEmailAsync(string email)
        {
            using IServiceScope scope = CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await context.Users.CountAsync(user => user.Email == email, TestContext.Current.CancellationToken);
        }

        public async Task<int> CountUsageEventsAsync()
        {
            using IServiceScope scope = CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await context.UsageEvents.CountAsync(TestContext.Current.CancellationToken);
        }

        public async Task<List<UsageEvent>> UsageEventsOfAsync(Guid userId)
        {
            using IServiceScope scope = CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await context.UsageEvents.AsNoTracking().Where(usageEvent => usageEvent.UserId == userId).ToListAsync(TestContext.Current.CancellationToken);
        }

        public async Task EndLockAsync(string email)
        {
            using IServiceScope scope = CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DateTimeOffset? oneMinuteAgo = DateTimeOffset.UtcNow.AddMinutes(-1);

            await context.Users
                .Where(user => user.Email == email)
                .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.LockoutEnd, oneMinuteAgo), TestContext.Current.CancellationToken);
        }

        public async Task MarkTimeZoneManualAsync(string email)
        {
            using IServiceScope scope = CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Users
                .Where(user => user.Email == email)
                .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.IsTimeZoneManual, true), TestContext.Current.CancellationToken);
        }

        protected static WebApplicationFactoryClientOptions HttpsWithCookies()
        {
            return new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true,
                AllowAutoRedirect = false,
            };
        }

        public static WebApplicationFactoryClientOptions HttpsWithoutCookies()
        {
            return new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = false,
                AllowAutoRedirect = false,
            };
        }
    }
}
