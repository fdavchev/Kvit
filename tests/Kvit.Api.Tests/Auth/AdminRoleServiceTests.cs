using Kvit.Domain.Accounts;
using Kvit.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class AdminRoleServiceTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        [Fact]
        public void Names_AreTheValuesStoredInTheDatabaseAndSentToTheWebsite()
        {
            Assert.Equal("Admin", Roles.Admin);
            Assert.Equal("ADMIN_STATS_VIEW", Permissions.AdminStatsView);
        }

        [Fact]
        public void Identity_HasRolesRegistered()
        {
            using IServiceScope scope = _app.CreateScope();

            RoleManager<IdentityRole<Guid>> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            Assert.NotNull(roleManager);
        }

        [Fact]
        public void AdminRoleService_IsRegisteredSoTheScriptCanGetItFromTheContainer()
        {
            using IServiceScope scope = _app.CreateScope();

            AdminRoleService service = scope.ServiceProvider.GetRequiredService<AdminRoleService>();

            Assert.NotNull(service);
        }

        [Fact]
        public async Task Grant_KnownEmail_ReturnsTrueAndStoresOneAdminRow()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);

            bool granted = await _app.GrantAdminAsync(form.Email);

            Assert.True(granted);
            Assert.Equal(1, await _app.CountAdminRowsOfAsync(form.Email));
        }

        [Fact]
        public async Task Grant_EmailInOtherCase_ReturnsTrueAndGrantsTheAccount()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);

            bool granted = await _app.GrantAdminAsync(form.Email.ToUpperInvariant());

            Assert.True(granted);
            Assert.Equal(1, await _app.CountAdminRowsOfAsync(form.Email));
        }

        [Fact]
        public async Task Grant_UnknownEmail_ReturnsFalseAndChangesNoAccount()
        {
            RegistrationForm bystander = RegistrationForm.Valid();
            await _app.CreateAccountAsync(bystander);
            int usersBefore = await _app.CountUsersAsync();

            bool granted = await _app.GrantAdminAsync(RegistrationForm.UniqueEmail());

            Assert.False(granted);
            Assert.Equal(usersBefore, await _app.CountUsersAsync());
            Assert.Equal(0, await _app.CountAdminRowsOfAsync(bystander.Email));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Grant_BlankEmail_ReturnsFalse(string email)
        {
            bool granted = await _app.GrantAdminAsync(email);

            Assert.False(granted);
        }

        [Fact]
        public async Task Grant_OneAccount_LeavesOtherAccountsWithoutTheRole()
        {
            RegistrationForm chosen = RegistrationForm.Valid();
            RegistrationForm bystander = RegistrationForm.Valid();
            await _app.CreateAccountAsync(chosen);
            await _app.CreateAccountAsync(bystander);

            await _app.MakeAdminAsync(chosen.Email);

            Assert.Equal(0, await _app.CountAdminRowsOfAsync(bystander.Email));
        }

        [Fact]
        public async Task Grant_CalledTwice_StaysTrueWithOneAdminRow()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            await _app.GrantAdminAsync(form.Email);

            bool grantedAgain = await _app.GrantAdminAsync(form.Email);

            Assert.True(grantedAgain);
            Assert.Equal(1, await _app.CountAdminRowsOfAsync(form.Email));
        }

        [Fact]
        public async Task Grant_TwoAccounts_ShareOneAdminRoleRow()
        {
            RegistrationForm first = RegistrationForm.Valid();
            RegistrationForm second = RegistrationForm.Valid();
            await _app.CreateAccountAsync(first);
            await _app.CreateAccountAsync(second);

            await _app.MakeAdminAsync(first.Email);
            await _app.MakeAdminAsync(second.Email);

            Assert.Equal(1, await _app.CountAdminRoleRowsAsync());
            Assert.Equal(1, await _app.CountAdminRowsOfAsync(first.Email));
            Assert.Equal(1, await _app.CountAdminRowsOfAsync(second.Email));
        }

        [Fact]
        public async Task Revoke_AdminAccount_ReturnsTrueAndRemovesTheAdminRow()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            await _app.MakeAdminAsync(form.Email);

            bool revoked = await _app.RevokeAdminAsync(form.Email);

            Assert.True(revoked);
            Assert.Equal(0, await _app.CountAdminRowsOfAsync(form.Email));
        }

        [Fact]
        public async Task Revoke_AccountThatIsNotAdmin_ReturnsTrueAndStaysWithoutTheRole()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);

            bool revoked = await _app.RevokeAdminAsync(form.Email);

            Assert.True(revoked);
            Assert.Equal(0, await _app.CountAdminRowsOfAsync(form.Email));
        }

        [Fact]
        public async Task Revoke_UnknownEmail_ReturnsFalse()
        {
            bool revoked = await _app.RevokeAdminAsync(RegistrationForm.UniqueEmail());

            Assert.False(revoked);
        }

        [Fact]
        public async Task Revoke_OneAdmin_LeavesTheOtherAdminInTheRole()
        {
            RegistrationForm revokedForm = RegistrationForm.Valid();
            RegistrationForm keptForm = RegistrationForm.Valid();
            await _app.CreateAccountAsync(revokedForm);
            await _app.CreateAccountAsync(keptForm);
            await _app.MakeAdminAsync(revokedForm.Email);
            await _app.MakeAdminAsync(keptForm.Email);

            await _app.RevokeAdminAsync(revokedForm.Email);

            Assert.Equal(1, await _app.CountAdminRowsOfAsync(keptForm.Email));
        }

        [Fact]
        public async Task Grant_AfterRevoke_GivesTheRoleAgain()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            await _app.MakeAdminAsync(form.Email);
            await _app.RevokeAdminAsync(form.Email);

            bool granted = await _app.GrantAdminAsync(form.Email);

            Assert.True(granted);
            Assert.Equal(1, await _app.CountAdminRowsOfAsync(form.Email));
        }
    }
}
