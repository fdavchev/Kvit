using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class MemberChangedMeanwhileTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        private const string FreeName = "Marko";

        [Fact]
        public async Task Join_TwoPeopleClaimTheSameFreeNameAtTheSameMoment_OneJoinsAndTheOtherAnswers404MemberNotFound()
        {
            (GroupSetup setup, Guid freeNameRowId) = await CreateGroupWithFreeNameAsync();
            SignedInUser first = await _app.SignUpAsync("Cvetan");
            SignedInUser second = await _app.SignUpAsync("Dimitar");

            HttpResponseMessage[] responses = await JoinBothAtOnceAsync(setup, freeNameRowId, first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.NotFound], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode != HttpStatusCode.OK), HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Join_TwoPeopleClaimTheSameFreeNameAtTheSameMoment_LeavesTheNameWithTheWinnerAndTheOtherOutOfTheGroup()
        {
            (GroupSetup setup, Guid freeNameRowId) = await CreateGroupWithFreeNameAsync();
            SignedInUser first = await _app.SignUpAsync("Cvetan");
            SignedInUser second = await _app.SignUpAsync("Dimitar");

            HttpResponseMessage[] responses = await JoinBothAtOnceAsync(setup, freeNameRowId, first, second);

            SignedInUser winner = responses[0].StatusCode == HttpStatusCode.OK ? first : second;
            SignedInUser loser = ReferenceEquals(winner, first) ? second : first;
            Assert.Equal(winner.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, freeNameRowId, "user_id"));
            Assert.Equal(1, await GroupRows.CountMemberRowsOfUserAsync(_app, setup.GroupId, winner.UserId));
            Assert.Equal(0, await GroupRows.CountMemberRowsOfUserAsync(_app, setup.GroupId, loser.UserId));
        }

        [Fact]
        public async Task Claim_TwoMembersTapThatsMeOnTheSameFreeNameAtTheSameMoment_OneGetsItAndTheOtherAnswers404MemberNotFound()
        {
            (GroupSetup setup, Guid freeNameRowId) = await CreateGroupWithFreeNameAsync();
            SignedInUser third = await AddThirdMemberAsync(setup);

            HttpResponseMessage[] responses = await ClaimBothAtOnceAsync(setup, freeNameRowId, setup.Member, third);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NotFound], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode != HttpStatusCode.NoContent), HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Claim_TwoMembersTapThatsMeOnTheSameFreeNameAtTheSameMoment_TheOtherKeepsTheirOwnRowAndTheNameStaysWithTheWinner()
        {
            (GroupSetup setup, Guid freeNameRowId) = await CreateGroupWithFreeNameAsync();
            SignedInUser third = await AddThirdMemberAsync(setup);
            Guid thirdRowId = await GroupRows.CurrentMemberIdAsync(_app, setup.GroupId, third.UserId);

            HttpResponseMessage[] responses = await ClaimBothAtOnceAsync(setup, freeNameRowId, setup.Member, third);

            bool memberWon = responses[0].StatusCode == HttpStatusCode.NoContent;
            SignedInUser winner = memberWon ? setup.Member : third;
            SignedInUser loser = memberWon ? third : setup.Member;
            Guid loserRowId = memberWon ? thirdRowId : setup.MemberRowId;
            Assert.Equal(winner.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, freeNameRowId, "user_id"));
            Assert.Equal(loserRowId, await GroupRows.CurrentMemberIdAsync(_app, setup.GroupId, loser.UserId));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, loserRowId, "end_kind"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberClaimed"));
        }

        [Fact]
        public async Task RemoveFreeName_WhileSomeoneJoinsOnItAtTheSameMoment_Answers204AndRemovesTheRowWithThePersonWhoTookIt()
        {
            (GroupSetup setup, Guid freeNameRowId) = await CreateGroupWithFreeNameAsync();
            SignedInUser joiner = await _app.SignUpAsync("Cvetan");
            await using InsertGate gate = await HoldAtActivityEventAsync("MemberRemoved");

            Task<HttpResponseMessage> remove = GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, freeNameRowId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            HttpResponseMessage join = await GroupRequests.JoinAsync(joiner.Client, setup.InviteToken, freeNameRowId);
            await gate.OpenAsync();
            HttpResponseMessage removed = await remove;

            Assert.Equal(HttpStatusCode.OK, join.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
            Assert.Equal(joiner.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, freeNameRowId, "user_id"));
            Assert.Equal("Removed", await GroupRows.MemberRowValueAsync(_app, freeNameRowId, "end_kind"));
        }

        [Fact]
        public async Task LetInAgain_WhileTheOwnerUndoesTheSameGuestsClaimAtTheSameMoment_Answers400MemberNotGuest()
        {
            (GroupSetup setup, Guid freeNameRowId) = await CreateGroupWithFreeNameAsync();
            await GuestSteps.TapFreeNameAsync(_app, setup.GroupId, freeNameRowId);
            await using InsertGate gate = await HoldAtActivityEventAsync("GuestLetInAgain");

            Task<HttpResponseMessage> letInAgain = GroupRequests.LetGuestInAgainAsync(setup.Owner.Client, setup.GroupId, freeNameRowId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            HttpResponseMessage undone = await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, freeNameRowId);
            await gate.OpenAsync();
            HttpResponseMessage letIn = await letInAgain;

            Assert.Equal(HttpStatusCode.NoContent, undone.StatusCode);
            await ProblemResponse.AssertAsync(letIn, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_GUEST);
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "GuestLetInAgain"));
        }

        [Fact]
        public async Task Leave_WhileTheOwnerLetsTheSameGuestInAgainAtTheSameMoment_Answers404GroupNotFound()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, setup.GroupId, "Cvetan");
            await using InsertGate gate = await HoldAtActivityEventAsync("MemberLeft");

            Task<HttpResponseMessage> leave = GroupRequests.LeaveAsync(guest.Client, setup.GroupId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            HttpResponseMessage letIn = await GroupRequests.LetGuestInAgainAsync(setup.Owner.Client, setup.GroupId, guest.RowId);
            await gate.OpenAsync();
            HttpResponseMessage left = await leave;

            Assert.Equal(HttpStatusCode.NoContent, letIn.StatusCode);
            await ProblemResponse.AssertAsync(left, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, guest.RowId, "removed_at"));
        }

        private async Task<(GroupSetup Setup, Guid FreeNameRowId)> CreateGroupWithFreeNameAsync()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid freeNameRowId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, FreeName, setup.Owner.UserId);

            return (setup, freeNameRowId);
        }

        private async Task<SignedInUser> AddThirdMemberAsync(GroupSetup setup)
        {
            SignedInUser third = await _app.SignUpAsync("Cvetan");
            await GroupRows.InsertMemberAsync(_app, setup.GroupId, third.UserId, third.Form.DisplayName, third.UserId);

            return third;
        }

        private async Task<HttpResponseMessage[]> JoinBothAtOnceAsync(GroupSetup setup, Guid freeNameRowId, SignedInUser first, SignedInUser second)
        {
            await using InsertGate gate = await HoldAtActivityEventAsync("MemberJoined");

            Task<HttpResponseMessage> firstJoin = GroupRequests.JoinAsync(first.Client, setup.InviteToken, freeNameRowId);
            Task<HttpResponseMessage> secondJoin = GroupRequests.JoinAsync(second.Client, setup.InviteToken, freeNameRowId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();

            return await Task.WhenAll(firstJoin, secondJoin);
        }

        private async Task<HttpResponseMessage[]> ClaimBothAtOnceAsync(GroupSetup setup, Guid freeNameRowId, SignedInUser first, SignedInUser second)
        {
            await using InsertGate gate = await HoldAtActivityEventAsync("MemberClaimed");

            Task<HttpResponseMessage> firstClaim = GroupRequests.ClaimAsync(first.Client, setup.GroupId, freeNameRowId);
            Task<HttpResponseMessage> secondClaim = GroupRequests.ClaimAsync(second.Client, setup.GroupId, freeNameRowId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();

            return await Task.WhenAll(firstClaim, secondClaim);
        }

        private Task<InsertGate> HoldAtActivityEventAsync(string eventType)
        {
            return InsertGate.CloseAsync(_app, "activity_events", $"NEW.type = '{eventType}'");
        }
    }
}
