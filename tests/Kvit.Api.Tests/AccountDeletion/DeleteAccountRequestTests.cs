using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class DeleteAccountRequestTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly DateTimeOffset RequestTime = new(2026, 11, 4, 9, 30, 15, TimeSpan.Zero);

        [Fact]
        public async Task Delete_KeepName_Answers204WithNoBody()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await DeletionRequests.DeleteAsync(user.Client, DeletionRequests.KeepName);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Delete_FormerMember_Answers204WithNoBody()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await DeletionRequests.DeleteAsync(user.Client, DeletionRequests.FormerMember);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Delete_KeepName_StoresTheChoiceAndTheTimeOfTheAppClockAndNoPurgeTime()
        {
            SignedInUser user = await _app.SignUpAsync();
            _app.Clock.SetTo(RequestTime);

            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);

            Assert.Equal(new StoredDeletion(RequestTime, DeletionRequests.KeepName, null), await DeletionRows.ReadAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Delete_FormerMember_StoresTheChoiceAndTheTimeOfTheAppClockAndNoPurgeTime()
        {
            SignedInUser user = await _app.SignUpAsync();
            _app.Clock.SetTo(RequestTime);

            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.FormerMember);

            Assert.Equal(new StoredDeletion(RequestTime, DeletionRequests.FormerMember, null), await DeletionRows.ReadAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Delete_Request_LeavesTheEmailAndThePasswordHashInPlace()
        {
            SignedInUser user = await _app.SignUpAsync();
            string? hashBefore = await DeletionRows.UserValueAsync(_app, user.UserId, "password_hash");

            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);

            Assert.Equal(user.Form.Email, await DeletionRows.UserValueAsync(_app, user.UserId, "email"));
            Assert.NotNull(hashBefore);
            Assert.Equal(hashBefore, await DeletionRows.UserValueAsync(_app, user.UserId, "password_hash"));
        }

        [Fact]
        public async Task Delete_Request_LeavesTheDisplayNameAndTheLockoutAlone()
        {
            SignedInUser user = await _app.SignUpAsync("Zorica");

            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.FormerMember);

            Assert.Equal("Zorica", await DeletionRows.UserValueAsync(_app, user.UserId, "display_name"));
            Assert.Null(await DeletionRows.UserValueAsync(_app, user.UserId, "lockout_end"));
        }

        [Fact]
        public async Task Delete_GoogleAccount_StoresTheRequest()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(identity);
            Guid userId = (await _app.FindUserAsync(identity.Email)).Id;
            _app.Clock.SetTo(RequestTime);

            await DeletionSteps.RequestAsync(client, DeletionRequests.KeepName);

            Assert.Equal(new StoredDeletion(RequestTime, DeletionRequests.KeepName, null), await DeletionRows.ReadAsync(_app, userId));
        }

        [Fact]
        public async Task Delete_Guest_Answers204AndStoresTheRequest()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            _app.Clock.SetTo(RequestTime);

            await DeletionSteps.RequestAsync(guest.Client, DeletionRequests.FormerMember);

            Assert.Equal(new StoredDeletion(RequestTime, DeletionRequests.FormerMember, null), await DeletionRows.ReadAsync(_app, guest.UserId));
        }

        [Fact]
        public async Task Delete_Anonymous_Answers401AndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpClient anonymous = _app.CreateClient();
            string before = await DeletionRows.SnapshotOfUserRowAsync(_app, user.UserId);

            HttpResponseMessage response = await DeletionRequests.DeleteAsync(anonymous, DeletionRequests.KeepName);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(before, await DeletionRows.SnapshotOfUserRowAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Delete_WithATemporaryPassword_Answers403MustChangePasswordAndStoresNothing()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);
            Guid userId = (await _app.FindUserAsync(account.Form.Email)).Id;

            HttpResponseMessage response = await DeletionRequests.DeleteAsync(client, DeletionRequests.KeepName);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.AUTH_MUST_CHANGE_PASSWORD);
            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, userId));
        }

        [Fact]
        public async Task Delete_Request_LeavesGroupsExpensesBudgetAndEverythingElseOutsideTheUsersTableUntouched()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await BudgetCurrencySteps.UseRateAsync(_app, PersonalDataSteps.MkdPerEur);
            ExpenseInput dinner = ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, dinner);
            await PersonalDataSteps.CreateFullSetAsync(_app, group.Owner, expenseId);
            string before = await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app);

            await DeletionSteps.RequestAsync(group.Owner.Client, DeletionRequests.FormerMember);

            Assert.Equal(before, await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app));
        }

        [Fact]
        public async Task Delete_Request_LeavesOtherAccountsUntouched()
        {
            SignedInUser leaver = await _app.SignUpAsync("Zorica");
            SignedInUser other = await _app.SignUpAsync("Bojan");
            string otherBefore = await DeletionRows.SnapshotOfRowsOfUserAsync(_app, other.UserId);

            await DeletionSteps.RequestAsync(leaver.Client, DeletionRequests.FormerMember);

            Assert.Equal(otherBefore, await DeletionRows.SnapshotOfRowsOfUserAsync(_app, other.UserId));
            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, other.UserId));
        }

        [Fact]
        public async Task Delete_Request_KeepsThePersonInTheirGroupForTheOthersWithTheSameName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string before = await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "name") ?? string.Empty;

            await DeletionSteps.RequestAsync(group.Member.Client, DeletionRequests.FormerMember);

            Assert.Equal(before, await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "name"));
            Assert.Equal(group.Member.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "user_id"));
            Assert.Contains(group.MemberRowId, await GroupRequests.MemberIdsAsync(group.Owner.Client, group.GroupId));
        }

        [Fact]
        public async Task Delete_Request_WritesNoActivityEventAndNoUsageEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            int activityBefore = await GroupRows.CountEventsAsync(_app, group.GroupId);
            int usageBefore = await _app.CountUsageEventsAsync();

            await DeletionSteps.RequestAsync(group.Member.Client, DeletionRequests.KeepName);

            Assert.Equal(activityBefore, await GroupRows.CountEventsAsync(_app, group.GroupId));
            Assert.Equal(usageBefore, await _app.CountUsageEventsAsync());
        }
    }
}
