using System.Net;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public static class GuestUpgradeChecks
    {
        public static async Task<string?> UserValueAsync(MigratedDatabase database, Guid userId, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM users WHERE id = @id",
                new NpgsqlParameter("id", userId));

            return Assert.Single(values);
        }

        public static async Task AssertRefusedAndStillAGuestAsync(MigratedDatabase database, HttpResponseMessage response, GuestMember guest, HttpStatusCode expectedStatus, string expectedErrorCode)
        {
            await ProblemResponse.AssertAsync(response, expectedStatus, expectedErrorCode);
            Assert.Null(AuthCookie.Find(response));
            await AssertStillAGuestAsync(database, guest);
        }

        public static async Task AssertStillAGuestAsync(MigratedDatabase database, GuestMember guest)
        {
            Assert.Equal(
                "true|true|true|true",
                await UserValueAsync(database, guest.UserId, "concat_ws('|', is_guest::text, (email IS NULL)::text, (normalized_email IS NULL)::text, (password_hash IS NULL)::text)"));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(database, guest.UserId, "GuestUpgraded"));
            HttpResponseMessage me = await AuthRequests.GetMeAsync(guest.Client);
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            Assert.True((await AuthRequests.ReadJsonAsync(me)).GetProperty("isGuest").GetBoolean());
        }
    }
}
