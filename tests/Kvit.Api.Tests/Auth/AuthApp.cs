using System.Net;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Postgres;
using Kvit.Domain.Entities;
using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
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

        public async Task<HttpClient> CreateRegisteredClientAsync(RegistrationForm form)
        {
            HttpClient client = CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return client;
        }

        public async Task<HttpClient> CreateLoggedInClientAsync(string email, string password)
        {
            HttpClient client = CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, email, password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return client;
        }

        public async Task<string?> ResetPasswordAsync(string email)
        {
            using IServiceScope scope = CreateScope();
            PasswordResetService service = ActivatorUtilities.CreateInstance<PasswordResetService>(scope.ServiceProvider);

            return await service.ResetAsync(email, TestContext.Current.CancellationToken);
        }

        public async Task<string> ResetExistingAccountAsync(string email)
        {
            return await ResetPasswordAsync(email)
                ?? throw new InvalidOperationException($"No account has the email '{email}', so no temporary password was made.");
        }

        public async Task<ResetAccount> CreateResetAccountAsync()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await CreateAccountAsync(form);
            string temporaryPassword = await ResetExistingAccountAsync(form.Email);

            return new ResetAccount(form, temporaryPassword);
        }

        public async Task<bool> MustChangePasswordAsync(string email)
        {
            List<string?> values = await QueryAsync(
                "SELECT must_change_password::text FROM users WHERE email = @email",
                new NpgsqlParameter("email", email));

            return bool.Parse(Assert.Single(values) ?? throw new InvalidOperationException($"must_change_password is null for '{email}'."));
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
