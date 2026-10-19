using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public static class GuestSteps
    {
        public static async Task<GuestMember> TapFreeNameAsync(GroupsApp app, Guid groupId, Guid freeNameRowId)
        {
            HttpClient client = app.CreateClient();

            HttpResponseMessage response = await GroupRequests.JoinAsGuestAsync(client, await InviteTokenOfAsync(app, groupId), claimMemberId: freeNameRowId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return new GuestMember(client, await UserIdOfAsync(client), freeNameRowId);
        }

        public static async Task<GuestMember> JoinWithNewNameAsync(GroupsApp app, Guid groupId, string name)
        {
            HttpClient client = app.CreateClient();

            HttpResponseMessage response = await GroupRequests.JoinAsGuestAsync(client, await InviteTokenOfAsync(app, groupId), name: name);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Guid userId = await UserIdOfAsync(client);
            return new GuestMember(client, userId, await GroupRows.CurrentMemberIdAsync(app, groupId, userId));
        }

        public static async Task<(ExpenseGroup Group, GuestMember Guest)> CreateExpenseGroupWithGuestAsync(ExpensesApp app, string freeName)
        {
            ExpenseGroup group = await app.CreateExpenseGroupAsync(freeName);
            GuestMember guest = await TapFreeNameAsync(app, group.GroupId, group.PlainRowIds[0]);

            return (group, guest);
        }

        public static async Task<string> InviteTokenOfAsync(GroupsApp app, Guid groupId)
        {
            return await GroupRows.GroupValueAsync(app, groupId, "invite_token")
                ?? throw new InvalidOperationException($"Group {groupId} has no invite token.");
        }

        private static async Task<Guid> UserIdOfAsync(HttpClient client)
        {
            HttpResponseMessage me = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            return (await AuthRequests.ReadJsonAsync(me)).GetProperty("id").GetGuid();
        }
    }
}
