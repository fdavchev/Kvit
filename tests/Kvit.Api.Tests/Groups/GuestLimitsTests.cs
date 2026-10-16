using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class GuestLimitsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string FreeName = "Marko";
        private const string StrongPassword = "Guest-Pass-2026";

        [Fact]
        public async Task CreateGroup_ByAGuest_Answers403GuestNotAllowedAndCreatesNoGroup()
        {
            (_, GuestMember guest) = await CreateGroupWithGuestAsync();

            HttpResponseMessage response = await GroupRequests.CreateAsync(guest.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.Equal(0, await GroupRows.CountGroupsOwnedByAsync(_app, guest.UserId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, guest.UserId, "GroupCreated"));
        }

        [Fact]
        public async Task CreateGroup_ByAGuestWithAnInvalidName_Answers403BeforeTheNameIsChecked()
        {
            (_, GuestMember guest) = await CreateGroupWithGuestAsync();

            HttpResponseMessage response = await GroupRequests.CreateAsync(guest.Client, name: string.Empty);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
        }

        [Fact]
        public async Task OneBill_ByAGuest_Answers403GuestNotAllowedAndWritesNothing()
        {
            (_, GuestMember guest) = await CreateGroupWithGuestAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Petre");

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(guest.Client, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.Equal(0, await GroupRows.CountGroupsOwnedByAsync(_app, guest.UserId));
            Assert.Equal(0, await ExpenseRows.CountExpensesCreatedByAsync(_app, guest.UserId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, guest.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task AddPlainName_ByAGuest_Answers403GuestNotAllowedAndWritesNothing()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();
            string before = await GroupRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(guest.Client, group.GroupId, "Grandma");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task AddPlainName_ByAGuestInAFinishedGroup_Answers403GuestNotAllowedBeforeTheGroupState()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();
            await _app.ExecuteAsync(
                "UPDATE groups SET status = 'Finished', closing_started_at = now(), finished_at = now() WHERE id = @id",
                new NpgsqlParameter("id", group.GroupId));

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(guest.Client, group.GroupId, "Grandma");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
        }

        [Fact]
        public async Task AddPlainName_ByAGuestToAGroupTheyAreNotIn_Answers404GroupNotFound()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(group.Owner);

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(guest.Client, otherGroupId, "Grandma");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task AddPlainName_ByAMemberWithAFullAccountInTheSameGroup_StillAnswers200()
        {
            (ExpenseGroup group, _) = await CreateGroupWithGuestAsync();

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(group.Member.Client, group.GroupId, "Grandma");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SetPassword_ByAGuest_Answers403GuestNotAllowedAndSetsNoPassword()
        {
            (_, GuestMember guest) = await CreateGroupWithGuestAsync();

            HttpResponseMessage response = await AuthRequests.SetPasswordAsync(guest.Client, StrongPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            List<string?> stored = await _app.QueryAsync(
                "SELECT concat_ws('|', is_guest::text, (password_hash IS NULL)::text) FROM users WHERE id = @id",
                new NpgsqlParameter("id", guest.UserId));
            Assert.Equal("true|true", Assert.Single(stored));
        }

        [Fact]
        public async Task SetPassword_ByAGuestWithATooWeakPassword_Answers403BeforeThePasswordRule()
        {
            (_, GuestMember guest) = await CreateGroupWithGuestAsync();

            HttpResponseMessage response = await AuthRequests.SetPasswordAsync(guest.Client, "weak");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
        }

        [Fact]
        public async Task SetPassword_ByAGuest_KeepsTheGuestSignedIn()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();

            await AuthRequests.SetPasswordAsync(guest.Client, StrongPassword);

            HttpResponseMessage me = await AuthRequests.GetMeAsync(guest.Client);
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(me);
            Assert.True(body.GetProperty("isGuest").GetBoolean());
            Assert.False(body.GetProperty("hasPassword").GetBoolean());
            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.GetAsync(guest.Client, group.GroupId)).StatusCode);
        }

        [Fact]
        public async Task DeleteExpense_ByAGuest_OfAnExpenseAnotherMemberAdded_Answers403ExpenseNotAllowedAndKeepsIt()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.MemberRowId, group.MemberRowId, guest.RowId));
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.DeleteAsync(guest.Client, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.EXPENSE_NOT_ALLOWED);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task DeleteExpense_ByAGuest_OfAnExpenseTheGuestAdded_Answers204AndDeletesIt()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();
            HttpResponseMessage added = await ExpenseRequests.AddAsync(guest.Client, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, guest.RowId, guest.RowId, group.MemberRowId));
            Assert.Equal(HttpStatusCode.OK, added.StatusCode);
            Guid expenseId = (await AuthRequests.ReadJsonAsync(added)).GetProperty("id").GetGuid();

            HttpResponseMessage response = await ExpenseRequests.DeleteAsync(guest.Client, group.GroupId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(guest.UserId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "deleted_by_user_id"));
        }

        private async Task<(ExpenseGroup Group, GuestMember Guest)> CreateGroupWithGuestAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync(FreeName);
            GuestMember guest = await GuestSteps.TapFreeNameAsync(_app, group.GroupId, group.PlainRowIds[0]);

            return (group, guest);
        }
    }
}
