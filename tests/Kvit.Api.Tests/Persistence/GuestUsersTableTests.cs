using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class GuestUsersTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string GuestConstraint = "ck_users_guest_has_no_email_or_password";

        [Fact]
        public async Task Guest_WithoutEmailOrPassword_IsAccepted()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser guest = NewGuest();
            context.Users.Add(guest);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            List<string?> stored = await _database.QueryAsync(
                "SELECT is_guest::text || '|' || (email IS NULL)::text || '|' || (password_hash IS NULL)::text FROM users WHERE id = @id",
                new NpgsqlParameter("id", guest.Id));
            Assert.Equal("true|true|true", Assert.Single(stored));
        }

        [Fact]
        public async Task Guest_WithAnEmail_ViolatesTheGuestCheckConstraint()
        {
            AppUser guest = NewGuest();
            guest.Email = $"guest-{Guid.NewGuid():N}@example.com";

            await AssertGuestConstraintViolatedAsync(guest);
        }

        [Fact]
        public async Task Guest_WithANormalizedEmail_ViolatesTheGuestCheckConstraint()
        {
            AppUser guest = NewGuest();
            guest.NormalizedEmail = $"GUEST-{Guid.NewGuid():N}@EXAMPLE.COM";

            await AssertGuestConstraintViolatedAsync(guest);
        }

        [Fact]
        public async Task Guest_WithAPasswordHash_ViolatesTheGuestCheckConstraint()
        {
            AppUser guest = NewGuest();
            guest.PasswordHash = "not-a-real-hash";

            await AssertGuestConstraintViolatedAsync(guest);
        }

        [Fact]
        public async Task User_SavedWithoutSettingIsGuest_IsStoredAsNotAGuest()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser user = TestUsers.Create();
            context.Users.Add(user);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            List<string?> stored = await _database.QueryAsync("SELECT is_guest::text FROM users WHERE id = @id", new NpgsqlParameter("id", user.Id));
            Assert.Equal("false", Assert.Single(stored));
        }

        [Fact]
        public async Task Migration_GuestAccounts_IsAppliedOnceAfterTheClosingResponsesOneAndLeavesNothingPending()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];
            IEnumerable<string> pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);

            Assert.Single(applied, migration => migration.EndsWith("_GuestAccounts", StringComparison.Ordinal));
            int closingResponses = applied.FindIndex(migration => migration.EndsWith("_ClosingResponses", StringComparison.Ordinal));
            int guestAccounts = applied.FindIndex(migration => migration.EndsWith("_GuestAccounts", StringComparison.Ordinal));
            Assert.True(guestAccounts > closingResponses, $"The GuestAccounts migration is at {guestAccounts} but ClosingResponses is at {closingResponses}.");
            Assert.Empty(pending);
        }

        private async Task AssertGuestConstraintViolatedAsync(AppUser guest)
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Users.Add(guest);

            PostgresException error = await PostgresErrors.SaveChangesFailingAsync(context);

            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
            Assert.Equal(GuestConstraint, error.ConstraintName);
        }

        private static AppUser NewGuest()
        {
            AppUser guest = TestUsers.Create();
            guest.UserName = $"guest-{guest.Id:N}";
            guest.NormalizedUserName = guest.UserName.ToUpperInvariant();
            guest.Email = null;
            guest.NormalizedEmail = null;
            guest.IsGuest = true;

            return guest;
        }
    }
}
