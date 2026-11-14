using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeGuestTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Purge_GuestKeepName_TheMemberRowBecomesAPlainNameWithTheGuestsName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, group.GroupId, "Cvetan");

            await DeletionSteps.RequestAndPurgeAsync(_app, guest.Client, guest.UserId, DeletionRequests.KeepName);

            Assert.Null(await GroupRows.MemberRowValueAsync(_app, guest.RowId, "user_id"));
            Assert.Equal("Cvetan", await GroupRows.MemberRowValueAsync(_app, guest.RowId, "name"));
        }

        [Fact]
        public async Task Purge_GuestFormerMember_TheMemberRowIsCalledFormerMember()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, group.GroupId, "Cvetan");

            await DeletionSteps.RequestAndPurgeAsync(_app, guest.Client, guest.UserId, DeletionRequests.FormerMember);

            Assert.Equal(DeletionRequests.FormerMemberEnglish, await GroupRows.MemberRowValueAsync(_app, guest.RowId, "name"));
        }

        [Fact]
        public async Task Purge_GuestWhoClaimedAName_TheClaimedRowBecomesAPlainName()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");

            await DeletionSteps.RequestAndPurgeAsync(_app, guest.Client, guest.UserId, DeletionRequests.FormerMember);

            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.PlainRowIds[0], "user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.PlainRowIds[0], "claimed_at"));
        }

        [Fact]
        public async Task Purge_Guest_TheGuestsCookieIsRefusedAfterwards()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            await DeletionSteps.RequestAndPurgeAsync(_app, guest.Client, guest.UserId, DeletionRequests.KeepName);

            HttpResponseMessage me = await AuthRequests.GetMeAsync(guest.Client);

            Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        }

        [Fact]
        public async Task Purge_Guest_OthersStillSeeTheGuestsNameInTheMemberList()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, group.GroupId, "Cvetan");

            await DeletionSteps.RequestAndPurgeAsync(_app, guest.Client, guest.UserId, DeletionRequests.KeepName);

            JsonElement row = await GroupRequests.MemberRowAsync(group.Owner.Client, group.GroupId, guest.RowId);
            Assert.Equal("Cvetan", row.GetProperty("displayName").GetString());
            Assert.True(row.GetProperty("isNameOnly").GetBoolean());
            Assert.False(row.GetProperty("isGuest").GetBoolean());
        }
    }
}
