using Kvit.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class UserValidationTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        [Fact]
        public async Task CreateGuest_WithoutAnEmail_Succeeds()
        {
            using IServiceScope scope = _app.CreateScope();
            UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            IdentityResult created = await users.CreateAsync(NewUser(email: null, isGuest: true));

            Assert.True(created.Succeeded, IdentityErrorCodes(created));
        }

        [Fact]
        public async Task CreateAccount_WithoutAnEmail_IsRefusedWithInvalidEmail()
        {
            using IServiceScope scope = _app.CreateScope();
            UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            IdentityResult created = await users.CreateAsync(NewUser(email: null, isGuest: false));

            Assert.False(created.Succeeded);
            Assert.Contains(nameof(IdentityErrorDescriber.InvalidEmail), IdentityErrorCodes(created));
        }

        [Fact]
        public async Task CreateAccount_WithAnEmailAnotherAccountHas_IsRefusedWithDuplicateEmail()
        {
            string email = RegistrationForm.UniqueEmail();
            using IServiceScope scope = _app.CreateScope();
            UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            Assert.True((await users.CreateAsync(NewUser(email, isGuest: false))).Succeeded);

            IdentityResult second = await users.CreateAsync(NewUser(email.ToUpperInvariant(), isGuest: false, userName: $"other-{Guid.NewGuid():N}"));

            Assert.False(second.Succeeded);
            Assert.Contains(nameof(IdentityErrorDescriber.DuplicateEmail), IdentityErrorCodes(second));
        }

        [Fact]
        public async Task CreateAccount_WithAnEmailThatIsNotValid_IsRefusedWithInvalidEmail()
        {
            using IServiceScope scope = _app.CreateScope();
            UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            IdentityResult created = await users.CreateAsync(NewUser("not-an-email", isGuest: false));

            Assert.False(created.Succeeded);
            Assert.Contains(nameof(IdentityErrorDescriber.InvalidEmail), IdentityErrorCodes(created));
        }

        [Fact]
        public async Task CreateAccount_WithAUserNameAnotherUserHas_IsRefusedWithDuplicateUserName()
        {
            string email = RegistrationForm.UniqueEmail();
            using IServiceScope scope = _app.CreateScope();
            UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            Assert.True((await users.CreateAsync(NewUser(email, isGuest: false))).Succeeded);

            IdentityResult second = await users.CreateAsync(NewUser(RegistrationForm.UniqueEmail(), isGuest: false, userName: email));

            Assert.False(second.Succeeded);
            Assert.Contains(nameof(IdentityErrorDescriber.DuplicateUserName), IdentityErrorCodes(second));
        }

        [Fact]
        public void UserManager_HasExactlyOneUserValidator()
        {
            using IServiceScope scope = _app.CreateScope();
            UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            Assert.Single(users.UserValidators);
        }

        private static AppUser NewUser(string? email, bool isGuest, string? userName = null)
        {
            AppUser user = new()
            {
                Email = email,
                DisplayName = "Ana",
                Language = "en",
                TimeZone = "Europe/Skopje",
                CreatedAt = DateTimeOffset.UtcNow,
                IsGuest = isGuest,
            };
            user.UserName = userName ?? email ?? $"guest-{user.Id:N}";

            return user;
        }

        private static string IdentityErrorCodes(IdentityResult result)
        {
            return string.Join(", ", result.Errors.Select(error => error.Code));
        }
    }
}
