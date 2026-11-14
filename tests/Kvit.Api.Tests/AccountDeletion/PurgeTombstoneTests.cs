using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeTombstoneTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan FiftyYears = TimeSpan.FromDays(365 * 50);

        [Fact]
        public async Task Purge_KeepName_KeepsTheOldDisplayName()
        {
            SignedInUser user = await _app.SignUpAsync("Zorica Petrovska");

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal("Zorica Petrovska", await DeletionRows.UserValueAsync(_app, user.UserId, "display_name"));
        }

        [Fact]
        public async Task Purge_FormerMemberInEnglish_SetsTheEnglishWord()
        {
            SignedInUser user = await _app.SignUpAsync("Zorica Petrovska");

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.FormerMember);

            Assert.Equal(DeletionRequests.FormerMemberEnglish, await DeletionRows.UserValueAsync(_app, user.UserId, "display_name"));
        }

        [Fact]
        public async Task Purge_FormerMemberInMacedonian_SetsTheMacedonianWord()
        {
            SignedInUser user = await _app.SignUpAsync("Зорица Петровска");
            await AuthRequests.ChangeLanguageAsync(user.Client, "mk");

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.FormerMember);

            Assert.Equal(DeletionRequests.FormerMemberMacedonian, await DeletionRows.UserValueAsync(_app, user.UserId, "display_name"));
        }

        [Fact]
        public async Task Purge_KeepNameInMacedonian_KeepsTheOldDisplayName()
        {
            SignedInUser user = await _app.SignUpAsync("Зорица Петровска");
            await AuthRequests.ChangeLanguageAsync(user.Client, "mk");

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal("Зорица Петровска", await DeletionRows.UserValueAsync(_app, user.UserId, "display_name"));
        }

        [Fact]
        public async Task Purge_ClearsTheEmailAndTheNormalizedEmail()
        {
            SignedInUser user = await _app.SignUpAsync();

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Null(await DeletionRows.UserValueAsync(_app, user.UserId, "email"));
            Assert.Null(await DeletionRows.UserValueAsync(_app, user.UserId, "normalized_email"));
        }

        [Fact]
        public async Task Purge_ClearsThePasswordHash()
        {
            SignedInUser user = await _app.SignUpAsync();

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Null(await DeletionRows.UserValueAsync(_app, user.UserId, "password_hash"));
        }

        [Fact]
        public async Task Purge_ClearsTheGooglePictureUrl()
        {
            GoogleAccount account = await DeletionSteps.SignUpWithGoogleAsync(_app);

            await DeletionSteps.RequestAndPurgeAsync(_app, account.Client, account.UserId, DeletionRequests.KeepName);

            Assert.Null(await DeletionRows.UserValueAsync(_app, account.UserId, "google_picture_url"));
        }

        [Fact]
        public async Task Purge_ClearsTheBudgetTotalCurrency()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.SetAsync(user, "EUR");
            Assert.Equal("EUR", await DeletionRows.UserValueAsync(_app, user.UserId, "budget_total_currency"));

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Null(await DeletionRows.UserValueAsync(_app, user.UserId, "budget_total_currency"));
        }

        [Fact]
        public async Task Purge_ClearsTheTermsVersionAndTheTimeTogether()
        {
            SignedInUser user = await _app.SignUpAsync();
            Assert.NotNull(await DeletionRows.UserValueAsync(_app, user.UserId, "terms_version"));

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Null(await DeletionRows.UserValueAsync(_app, user.UserId, "terms_version"));
            Assert.Null(await DeletionRows.UserValueAsync(_app, user.UserId, "terms_accepted_at"));
        }

        [Fact]
        public async Task Purge_SetsTheUserNameToDeletedAndTheIdWithoutDashes()
        {
            SignedInUser user = await _app.SignUpAsync();

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal($"deleted-{user.UserId:N}", await DeletionRows.UserValueAsync(_app, user.UserId, "user_name"));
        }

        [Fact]
        public async Task Purge_SetsTheNormalizedUserNameToTheSameTextIgnoringCase()
        {
            SignedInUser user = await _app.SignUpAsync();

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            string? normalized = await DeletionRows.UserValueAsync(_app, user.UserId, "normalized_user_name");
            Assert.Equal($"deleted-{user.UserId:N}", normalized, ignoreCase: true);
        }

        [Fact]
        public async Task Purge_LocksTheAccountForever()
        {
            SignedInUser user = await _app.SignUpAsync();

            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal("true", await DeletionRows.UserValueAsync(_app, user.UserId, "lockout_enabled"));
            DateTimeOffset? lockoutEnd = await DeletionRows.TimeOfAsync(_app, "users WHERE id = @id", "lockout_end", new NpgsqlParameter("id", user.UserId));
            Assert.NotNull(lockoutEnd);
            Assert.True(lockoutEnd > now + FiftyYears, $"The lockout ends at {lockoutEnd:O}, which is less than 50 years after the purge.");
        }

        [Fact]
        public async Task Purge_RotatesTheSecurityStamp()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);
            string? stampAfterRequest = await DeletionRows.UserValueAsync(_app, user.UserId, "security_stamp");

            await AccountPurges.PurgeAsync(_app, user.UserId, DeletionRequests.KeepName, _app.Clock.GetUtcNow() + DeletionRequests.GraceTime);

            Assert.NotEqual(stampAfterRequest, await DeletionRows.UserValueAsync(_app, user.UserId, "security_stamp"));
        }

        [Fact]
        public async Task Purge_AccountWithATemporaryPassword_ResetsMustChangePasswordToFalse()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);
            await _app.ExecuteAsync("UPDATE users SET must_change_password = true WHERE id = @id", new NpgsqlParameter("id", user.UserId));

            await AccountPurges.PurgeAsync(_app, user.UserId, DeletionRequests.KeepName, _app.Clock.GetUtcNow() + DeletionRequests.GraceTime);

            Assert.Equal("false", await DeletionRows.UserValueAsync(_app, user.UserId, "must_change_password"));
        }

        [Fact]
        public async Task Purge_SetsPurgedAtToTheGivenTime()
        {
            SignedInUser user = await _app.SignUpAsync();

            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal(now, (await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        [Fact]
        public async Task Purge_KeepsTheRequestTimeAndTheChoiceAsEvidence()
        {
            SignedInUser user = await _app.SignUpAsync();
            _app.Clock.SetTo(ExpensesApp.ClockStart);

            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.FormerMember);

            Assert.Equal(new StoredDeletion(ExpensesApp.ClockStart, DeletionRequests.FormerMember, now), await DeletionRows.ReadAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Purge_KeepsTheUsersRowItselfAndItsCreationTime()
        {
            SignedInUser user = await _app.SignUpAsync();
            string? createdBefore = await DeletionRows.UserValueAsync(_app, user.UserId, "created_at");

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal(1, await DeletionRows.CountUsersWithIdAsync(_app, user.UserId));
            Assert.Equal(createdBefore, await DeletionRows.UserValueAsync(_app, user.UserId, "created_at"));
        }

        [Fact]
        public async Task Purge_RemovesTheGoogleLogin()
        {
            GoogleAccount account = await DeletionSteps.SignUpWithGoogleAsync(_app);
            Assert.Equal(1, await DeletionRows.CountOfUserAsync(_app, "user_logins", "user_id", account.UserId));

            await DeletionSteps.RequestAndPurgeAsync(_app, account.Client, account.UserId, DeletionRequests.KeepName);

            Assert.Equal(0, await DeletionRows.CountOfUserAsync(_app, "user_logins", "user_id", account.UserId));
        }

        [Fact]
        public async Task Purge_RemovesTheAdminRoleButNotTheRoleItself()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.MakeAdminAsync(user.Form.Email);
            Assert.Equal(1, await DeletionRows.CountOfUserAsync(_app, "user_roles", "user_id", user.UserId));

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal(0, await DeletionRows.CountOfUserAsync(_app, "user_roles", "user_id", user.UserId));
            Assert.Equal(1, await _app.CountAdminRoleRowsAsync());
        }

        [Fact]
        public async Task Purge_RemovesClaimsAndTokens()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionRows.AddClaimAndTokenAsync(_app, user.UserId);

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal(0, await DeletionRows.CountOfUserAsync(_app, "user_claims", "user_id", user.UserId));
            Assert.Equal(0, await DeletionRows.CountOfUserAsync(_app, "user_tokens", "user_id", user.UserId));
        }

        [Fact]
        public async Task Purge_LeavesAnotherAccountsRowsAndLoginsUntouched()
        {
            SignedInUser leaver = await _app.SignUpAsync("Zorica");
            GoogleAccount other = await DeletionSteps.SignUpWithGoogleAsync(_app);
            await _app.MakeAdminAsync(other.Identity.Email);
            string otherBefore = await DeletionRows.SnapshotOfRowsOfUserAsync(_app, other.UserId);

            await DeletionSteps.RequestAndPurgeAsync(_app, leaver.Client, leaver.UserId, DeletionRequests.FormerMember);

            Assert.Equal(otherBefore, await DeletionRows.SnapshotOfRowsOfUserAsync(_app, other.UserId));
        }

        [Fact]
        public async Task Purge_ForcedOnAnAccountThatNeverRequestedADeletion_StoresTheRequestTimeAndChoiceToo()
        {
            SignedInUser user = await _app.SignUpAsync();
            DateTimeOffset now = ExpensesApp.ClockStart + TimeSpan.FromDays(3);

            await AccountPurges.PurgeAsync(_app, user.UserId, DeletionRequests.FormerMember, now);

            Assert.Equal(new StoredDeletion(now, DeletionRequests.FormerMember, now), await DeletionRows.ReadAsync(_app, user.UserId));
            Assert.Equal(DeletionRequests.FormerMemberEnglish, await DeletionRows.UserValueAsync(_app, user.UserId, "display_name"));
        }

        [Fact]
        public async Task Purge_ForcedOnAnAccountWithARequest_KeepsTheOriginalRequestTime()
        {
            SignedInUser user = await _app.SignUpAsync();
            _app.Clock.SetTo(ExpensesApp.ClockStart);
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);
            DateTimeOffset now = ExpensesApp.ClockStart + TimeSpan.FromDays(3);

            await AccountPurges.PurgeAsync(_app, user.UserId, DeletionRequests.KeepName, now);

            Assert.Equal(new StoredDeletion(ExpensesApp.ClockStart, DeletionRequests.KeepName, now), await DeletionRows.ReadAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Purge_GuestWithNoEmail_BecomesATombstoneToo()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");

            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, guest.Client, guest.UserId, DeletionRequests.FormerMember);

            Assert.Equal($"deleted-{guest.UserId:N}", await DeletionRows.UserValueAsync(_app, guest.UserId, "user_name"));
            Assert.Equal(DeletionRequests.FormerMemberEnglish, await DeletionRows.UserValueAsync(_app, guest.UserId, "display_name"));
            Assert.Equal(now, (await DeletionRows.ReadAsync(_app, guest.UserId)).PurgedAt);
        }
    }
}
