using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;
using Kvit.Domain.Tests.Entities;
using Kvit.Domain.Tests.Fakes;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Services.Groups
{
    public class LetGuestInAgainTests
    {
        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly FakeClosingResponseRepository _closingResponses = new();
        private readonly LetGuestInAgain _letGuestInAgain;
        private readonly Group _group = GroupTestData.NewGroup();
        private readonly GroupMember _ownerRow;
        private readonly GroupMember _guestRow;

        public LetGuestInAgainTests()
        {
            _ownerRow = MemberTestData.Account(_group.Id, GroupTestData.OwnerId, "Filip");
            _guestRow = MemberTestData.Account(_group.Id, GroupTestData.MemberId, "Ana");
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _members.Seed(_ownerRow);
            _members.SeedGuest(_guestRow);
            FixedTimeProvider clock = new(GroupTestData.Moment);
            _letGuestInAgain = new LetGuestInAgain(_groups, _members, _activityEvents, new CancelClosingOnChange(_closingResponses, _activityEvents, clock), clock);
        }

        [Fact]
        public async Task Execute_OwnerLetsAGuestInAgain_TurnsTheSameRowIntoAPlainNameThatKeepsItsName()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, _guestRow.Id);

            Assert.True(result.IsSuccess);
            Assert.Null(_guestRow.UserId);
            Assert.Null(_guestRow.ClaimedAt);
            Assert.Equal("Ana", _guestRow.Name);
            Assert.Null(_guestRow.RemovedAt);
            Assert.Null(_guestRow.EndKind);
            Assert.Empty(_members.Added);
        }

        [Fact]
        public async Task Execute_AGuestWhoTookAName_TurnsTheTakenNameBackIntoAPlainName()
        {
            GroupMember claimedByGuest = MemberTestData.Claimed(_group.Id, MemberTestData.OtherUserId, "Grandma");
            _members.SeedGuest(claimedByGuest);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, claimedByGuest.Id);

            Assert.True(result.IsSuccess);
            Assert.Null(claimedByGuest.UserId);
            Assert.Null(claimedByGuest.ClaimedAt);
            Assert.Equal("Grandma", claimedByGuest.Name);
            Assert.Empty(_members.Added);
        }

        [Fact]
        public async Task Execute_OwnerLetsAGuestInAgain_WritesOneGuestLetInAgainEventByTheOwnerAboutTheRow()
        {
            await ExecuteAsync(GroupTestData.OwnerId, _guestRow.Id);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.GuestLetInAgain, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(_guestRow.Id, activityEvent.MemberId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal("Ana", ActivityEventData.Name(activityEvent));
        }

        [Fact]
        public async Task Execute_WhileTheGroupIsClosing_CancelsTheClosingAndFreesTheName()
        {
            _group.StartClosing(GroupTestData.Moment.AddHours(-1));

            Result result = await ExecuteAsync(GroupTestData.OwnerId, _guestRow.Id);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupStatus.Open, _group.Status);
            Assert.Null(_group.ClosingStartedAt);
            Assert.Null(_guestRow.UserId);
            Assert.Equal([ActivityEventType.ClosingCancelled, ActivityEventType.GuestLetInAgain], [.. _activityEvents.Added.Select(activityEvent => activityEvent.Type)]);
        }

        [Fact]
        public async Task Execute_AFinishedGroup_FailsWith400GroupFinishedAndChangesNothing()
        {
            _group.Finish(GroupTestData.Moment.AddDays(-1));

            Result result = await ExecuteAsync(GroupTestData.OwnerId, _guestRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
            AssertGuestStillOnTheRow();
        }

        [Fact]
        public async Task Execute_ANonMember_FailsWith404GroupNotFoundAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.StrangerId, _guestRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertGuestStillOnTheRow();
        }

        [Fact]
        public async Task Execute_TheGuestThemselves_FailsWith403GroupNotOwnerAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, _guestRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            AssertGuestStillOnTheRow();
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwnerAndAnUnknownTarget_FailsWith403BeforeTheTargetIsLookedUp()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, Guid.NewGuid());

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwnerInAFinishedGroup_FailsWith403BeforeTheGroupStateIsChecked()
        {
            _group.Finish(GroupTestData.Moment.AddDays(-1));

            Result result = await ExecuteAsync(GroupTestData.MemberId, _guestRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Execute_AnUnknownTarget_FailsWith404MemberNotFound()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, Guid.NewGuid());

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            AssertGuestStillOnTheRow();
        }

        [Fact]
        public async Task Execute_ARemovedGuestRow_FailsWith404MemberNotFoundAndKeepsTheRow()
        {
            GroupMember removedGuest = MemberTestData.Removed(_group.Id, MemberTestData.OtherUserId, "Cveta");
            _members.SeedGuest(removedGuest);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, removedGuest.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(MemberTestData.OtherUserId, removedGuest.UserId);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_APlainName_FailsWith400MemberNotGuest()
        {
            GroupMember plainName = MemberTestData.PlainName(_group.Id, "Marko");
            _members.Seed(plainName);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, plainName.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_GUEST);
            AssertGuestStillOnTheRow();
        }

        [Fact]
        public async Task Execute_ARowOfAPersonWithAFullAccount_FailsWith400MemberNotGuestAndKeepsTheAccount()
        {
            GroupMember accountRow = MemberTestData.Account(_group.Id, MemberTestData.OtherUserId, "Cveta");
            _members.Seed(accountRow);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, accountRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_GUEST);
            Assert.Equal(MemberTestData.OtherUserId, accountRow.UserId);
            AssertGuestStillOnTheRow();
        }

        [Fact]
        public async Task Execute_TheOwnersOwnRow_FailsWith400MemberNotGuest()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, _ownerRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_GUEST);
            Assert.Equal(GroupTestData.OwnerId, _ownerRow.UserId);
            AssertGuestStillOnTheRow();
        }

        private Task<Result> ExecuteAsync(Guid userId, Guid memberId)
        {
            return _letGuestInAgain.Execute(_group.Id, userId, memberId, TestContext.Current.CancellationToken);
        }

        private void AssertGuestStillOnTheRow()
        {
            Assert.Equal(GroupTestData.MemberId, _guestRow.UserId);
            Assert.Empty(_members.Added);
            Assert.Empty(_activityEvents.Added);
        }
    }
}
