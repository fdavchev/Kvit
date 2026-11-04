using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Postgres;
using Kvit.Domain.Entities;
using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.AdminStats
{
    public class AdminStatsApp : AuthApp
    {
        public static readonly DateTimeOffset ClockStart = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

        public AdminStatsApp(PostgresFixture postgres)
            : base(postgres)
        {
            Clock = new AdjustableClock(ClockStart);
            _factory.AdditionalTestServices = services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Clock);
            };
        }

        public AdjustableClock Clock { get; }

        public async Task ResetAsync()
        {
            Clock.SetTo(ClockStart);
            await ExecuteAsync("DELETE FROM usage_events");
        }

        public async Task AddEventsAsync(params UsageEvent[] events)
        {
            using IServiceScope scope = CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.UsageEvents.AddRange(events);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        public Task<Guid> AddPersonAsync()
        {
            return UserRows.AddAsync(this);
        }

        public async Task<Guid> AddGuestAsync()
        {
            using IServiceScope scope = CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser guest = TestUsers.Create();
            guest.Email = null;
            guest.NormalizedEmail = null;
            guest.IsGuest = true;
            context.Users.Add(guest);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return guest.Id;
        }

        public Task DeleteUserRowAsync(Guid userId)
        {
            return ExecuteAsync("DELETE FROM users WHERE id = @id", new NpgsqlParameter("id", userId));
        }

        public async Task<int> CountUsageEventsWithoutUserAsync()
        {
            List<string?> counts = await QueryAsync("SELECT count(*)::text FROM usage_events WHERE user_id IS NULL");

            return int.Parse(Assert.Single(counts)!);
        }

        public async Task<SignedInUser> SignInAdminAsync()
        {
            RegistrationForm form = RegistrationForm.Valid();
            Guid userId = await CreateAccountAsync(form);
            await MakeAdminAsync(form.Email);
            HttpClient client = await CreateLoggedInClientAsync(form.Email, form.Password);

            return new SignedInUser(client, userId, form);
        }
    }
}
