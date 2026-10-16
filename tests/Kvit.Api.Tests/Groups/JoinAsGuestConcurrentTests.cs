using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class JoinAsGuestConcurrentTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        private const string GuestRowsOnly = "NEW.is_guest";

        [Fact]
        public async Task JoinAsGuest_TwoGuestsTapTheSameFreeNameAtTheSameMoment_OneJoinsAndTheOtherAnswers404MemberNotFound()
        {
            (GroupSetup setup, Guid freeNameRowId) = await CreateGroupWithFreeNameAsync();
            HttpClient first = _app.CreateClient();
            HttpClient second = _app.CreateClient();

            HttpResponseMessage[] responses = await JoinBothAtOnceAsync(setup, freeNameRowId, first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.NotFound], [.. responses.Select(response => response.StatusCode).Order()]);
            HttpResponseMessage refused = responses.Single(response => response.StatusCode != HttpStatusCode.OK);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Null(AuthCookie.Find(refused));
        }

        [Fact]
        public async Task JoinAsGuest_TwoGuestsTapTheSameFreeNameAtTheSameMoment_LeavesTheNameWithTheWinnerAndNoGuestWithoutARow()
        {
            (GroupSetup setup, Guid freeNameRowId) = await CreateGroupWithFreeNameAsync();
            HttpClient first = _app.CreateClient();
            HttpClient second = _app.CreateClient();
            int guestsBefore = await CountGuestsAsync();

            HttpResponseMessage[] responses = await JoinBothAtOnceAsync(setup, freeNameRowId, first, second);

            HttpClient winner = responses[0].StatusCode == HttpStatusCode.OK ? first : second;
            HttpClient loser = ReferenceEquals(winner, first) ? second : first;
            HttpResponseMessage winnerMe = await AuthRequests.GetMeAsync(winner);
            Assert.Equal(HttpStatusCode.OK, winnerMe.StatusCode);
            Guid winnerId = (await AuthRequests.ReadJsonAsync(winnerMe)).GetProperty("id").GetGuid();
            Assert.Equal(winnerId.ToString(), await GroupRows.MemberRowValueAsync(_app, freeNameRowId, "user_id"));
            Assert.Equal(guestsBefore + 1, await CountGuestsAsync());
            Assert.Equal(HttpStatusCode.Unauthorized, (await AuthRequests.GetMeAsync(loser)).StatusCode);
            Assert.Equal(1, await GroupRows.CountMemberRowsOfUserAsync(_app, setup.GroupId, winnerId));
        }

        private async Task<(GroupSetup Setup, Guid FreeNameRowId)> CreateGroupWithFreeNameAsync()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid freeNameRowId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);

            return (setup, freeNameRowId);
        }

        private async Task<HttpResponseMessage[]> JoinBothAtOnceAsync(GroupSetup setup, Guid freeNameRowId, HttpClient first, HttpClient second)
        {
            await using InsertGate gate = await InsertGate.CloseAsync(_app, "users", GuestRowsOnly);

            Task<HttpResponseMessage> firstJoin = GroupRequests.JoinAsGuestAsync(first, setup.InviteToken, claimMemberId: freeNameRowId);
            Task<HttpResponseMessage> secondJoin = GroupRequests.JoinAsGuestAsync(second, setup.InviteToken, claimMemberId: freeNameRowId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();

            return await Task.WhenAll(firstJoin, secondJoin);
        }

        private async Task<int> CountGuestsAsync()
        {
            List<string?> counts = await _app.QueryAsync("SELECT count(*)::text FROM users WHERE is_guest");

            return int.Parse(Assert.Single(counts)!);
        }
    }
}
