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
    public class JoinGroupTests
    {
        private const string JoinerName = "Bojan";

        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly FakeUsageEventRepository _usageEvents = new();
        private readonly JoinGroup _joinGroup;
        private readonly Group _group = GroupTestData.NewGroup();
        private readonly GroupMember _plainNameRow;

        public JoinGroupTests()
        {
            _plainNameRow = MemberTestData.PlainName(_group.Id, "Grandma");
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _members.Seed(MemberTestData.Account(_group.Id, GroupTestData.OwnerId, "Filip"));
            _members.Seed(MemberTestData.Account(_group.Id, GroupTestData.MemberId, "Ana"));
            _members.Seed(_plainNameRow);
            _joinGroup = new JoinGroup(_groups, _members, _activityEvents, _usageEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_ANewPerson_ReturnsTheGroupIdAndAddsARowUnderTheirAccountName()
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, null);

            Assert.True(result.IsSuccess);
            Assert.Equal(_group.Id, result.Value);
            GroupMember added = Assert.Single(_members.Added);
            Assert.Equal(_group.Id, added.GroupId);
            Assert.Equal(GroupTestData.StrangerId, added.UserId);
            Assert.Equal(JoinerName, added.Name);
            Assert.Equal(GroupTestData.StrangerId, added.AddedByUserId);
            Assert.Equal(GroupTestData.Moment, added.JoinedAt);
            Assert.Null(added.ClaimedAt);
        }

        [Fact]
        public async Task Execute_ANewPerson_WritesOneMemberJoinedEventForTheNewRow()
        {
            await ExecuteAsync(GroupTestData.StrangerId, null);

            GroupMember added = Assert.Single(_members.Added);
            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.MemberJoined, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.StrangerId, activityEvent.ActorUserId);
            Assert.Equal(added.Id, activityEvent.MemberId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal(JoinerName, ActivityEventData.Name(activityEvent));
        }

        [Fact]
        public async Task Execute_ANewPerson_WritesOneJoinedViaInviteUsageEvent()
        {
            await ExecuteAsync(GroupTestData.StrangerId, null);

            UsageEvent usageEvent = Assert.Single(_usageEvents.Added);
            Assert.Equal(UsageEventType.JoinedViaInvite, usageEvent.Type);
            Assert.Equal(GroupTestData.StrangerId, usageEvent.UserId);
            Assert.Equal(GroupTestData.Moment, usageEvent.OccurredAt);
        }

        [Fact]
        public async Task Execute_ANewPersonWhoseNameMatchesAPlainName_JoinsWithoutClaimingAndBothRowsShowTheName()
        {
            Result<Guid> result = await _joinGroup.Execute(GroupTestData.InviteToken, GroupTestData.StrangerId, "grandma", null, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal("grandma", Assert.Single(_members.Added).Name);
            Assert.Null(_plainNameRow.UserId);
        }

        [Theory]
        [InlineData("no-such-token")]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Execute_AnUnknownOrBlankToken_FailsWith404InviteNotFoundAndWritesNothing(string token)
        {
            Result<Guid> result = await _joinGroup.Execute(token, GroupTestData.StrangerId, JoinerName, null, TestContext.Current.CancellationToken);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_TheTokenOfADeletedGroup_FailsWith404InviteNotFound()
        {
            _group.Delete(GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-1));

            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, null);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_TheOldTokenAfterAReset_FailsWith404InviteNotFound()
        {
            _group.ResetInvite("token-after-the-reset", GroupTestData.Moment.AddHours(-1));

            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, null);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_AlreadyACurrentMember_ReturnsTheGroupIdAndWritesNothing()
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.MemberId, null);

            Assert.True(result.IsSuccess);
            Assert.Equal(_group.Id, result.Value);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_AlreadyACurrentMemberWhoAlsoSendsAClaim_IgnoresTheClaim()
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.MemberId, _plainNameRow.Id);

            Assert.True(result.IsSuccess);
            Assert.Null(_plainNameRow.UserId);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_AlreadyACurrentMemberWhoAlsoSendsAnUnknownClaimTarget_StillSucceedsAndWritesNothing()
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.MemberId, Guid.NewGuid());

            Assert.True(result.IsSuccess);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_APersonWhoLeft_ComesBackOnTheSameRowWithoutANewOne()
        {
            GroupMember leftRow = MemberTestData.Left(_group.Id, GroupTestData.StrangerId, JoinerName);
            _members.Seed(leftRow);

            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, null);

            Assert.True(result.IsSuccess);
            Assert.Equal(_group.Id, result.Value);
            Assert.Null(leftRow.RemovedAt);
            Assert.Null(leftRow.RemovedByUserId);
            Assert.Null(leftRow.EndKind);
            Assert.Empty(_members.Added);
        }

        [Fact]
        public async Task Execute_APersonWhoLeft_WritesAMemberJoinedEventForTheOldRowAndAJoinedViaInviteUsageEvent()
        {
            GroupMember leftRow = MemberTestData.Left(_group.Id, GroupTestData.StrangerId, JoinerName);
            _members.Seed(leftRow);

            await ExecuteAsync(GroupTestData.StrangerId, null);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.MemberJoined, activityEvent.Type);
            Assert.Equal(leftRow.Id, activityEvent.MemberId);
            Assert.Equal(UsageEventType.JoinedViaInvite, Assert.Single(_usageEvents.Added).Type);
        }

        [Fact]
        public async Task Execute_APersonWhoLeftOnAClaimedName_ComesBackOnTheClaimedRow()
        {
            GroupMember claimedRow = MemberTestData.Claimed(_group.Id, GroupTestData.StrangerId, "Marko");
            claimedRow.Leave(GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-1));
            _members.Seed(claimedRow);

            await ExecuteAsync(GroupTestData.StrangerId, null);

            Assert.Null(claimedRow.EndKind);
            Assert.Equal(GroupTestData.StrangerId, claimedRow.UserId);
            Assert.NotNull(claimedRow.ClaimedAt);
            Assert.Empty(_members.Added);
        }

        [Fact]
        public async Task Execute_APersonWhoWasRemoved_FailsWith403InviteRemovedAndWritesNothing()
        {
            GroupMember removedRow = MemberTestData.Removed(_group.Id, GroupTestData.StrangerId, JoinerName);
            _members.Seed(removedRow);

            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, null);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.INVITE_REMOVED);
            Assert.Equal(MemberEndKind.Removed, removedRow.EndKind);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_APersonWhoWasRemovedAndSendsAClaim_FailsWith403BeforeAnyClaimRule()
        {
            _members.Seed(MemberTestData.Removed(_group.Id, GroupTestData.StrangerId, JoinerName));

            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, _plainNameRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.INVITE_REMOVED);
            Assert.Null(_plainNameRow.UserId);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_ANewPersonClaimsAPlainName_PutsTheirAccountOnTheNameAndAddsNoRowOfTheirOwn()
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, _plainNameRow.Id);

            Assert.True(result.IsSuccess);
            Assert.Equal(_group.Id, result.Value);
            Assert.Equal(GroupTestData.StrangerId, _plainNameRow.UserId);
            Assert.Equal(GroupTestData.Moment, _plainNameRow.ClaimedAt);
            Assert.Equal("Grandma", _plainNameRow.Name);
            Assert.Empty(_members.Added);
        }

        [Fact]
        public async Task Execute_ANewPersonClaimsAPlainName_WritesMemberJoinedThenMemberClaimedForTheClaimedRow()
        {
            await ExecuteAsync(GroupTestData.StrangerId, _plainNameRow.Id);

            Assert.Equal(2, _activityEvents.Added.Count);
            ActivityEvent joined = _activityEvents.Added[0];
            ActivityEvent claimed = _activityEvents.Added[1];
            Assert.Equal(ActivityEventType.MemberJoined, joined.Type);
            Assert.Equal(_plainNameRow.Id, joined.MemberId);
            Assert.Equal(GroupTestData.StrangerId, joined.ActorUserId);
            Assert.Equal(JoinerName, ActivityEventData.Name(joined));
            Assert.Equal(ActivityEventType.MemberClaimed, claimed.Type);
            Assert.Equal(_plainNameRow.Id, claimed.MemberId);
            Assert.Equal(GroupTestData.StrangerId, claimed.ActorUserId);
            Assert.Equal(JoinerName, ActivityEventData.Name(claimed));
            Assert.Equal("Grandma", ActivityEventData.ClaimedName(claimed));
        }

        [Fact]
        public async Task Execute_ANewPersonClaimsAPlainName_WritesOneJoinedViaInviteUsageEvent()
        {
            await ExecuteAsync(GroupTestData.StrangerId, _plainNameRow.Id);

            UsageEvent usageEvent = Assert.Single(_usageEvents.Added);
            Assert.Equal(UsageEventType.JoinedViaInvite, usageEvent.Type);
            Assert.Equal(GroupTestData.StrangerId, usageEvent.UserId);
        }

        [Fact]
        public async Task Execute_AnUnknownClaimTarget_FailsWith404MemberNotFoundAndWritesNothing()
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, Guid.NewGuid());

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_ARemovedPlainNameAsClaimTarget_FailsWith404MemberNotFound()
        {
            GroupMember removed = MemberTestData.RemovedPlainName(_group.Id, "Aunt");
            _members.Seed(removed);

            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, removed.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Null(removed.UserId);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_AnAccountRowAsClaimTarget_FailsWith404MemberNotFound()
        {
            GroupMember accountRow = MemberTestData.Account(_group.Id, MemberTestData.OtherUserId, "Dana");
            _members.Seed(accountRow);

            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, accountRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_AlreadyClaimedNameAsClaimTarget_FailsWith404MemberNotFound()
        {
            GroupMember claimed = MemberTestData.Claimed(_group.Id, MemberTestData.OtherUserId, "Marko");
            _members.Seed(claimed);

            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, claimed.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(MemberTestData.OtherUserId, claimed.UserId);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_APersonWhoLeftAndSendsAClaim_FailsWith400MemberCannotClaimAndStaysOutside()
        {
            GroupMember leftRow = MemberTestData.Left(_group.Id, GroupTestData.StrangerId, JoinerName);
            _members.Seed(leftRow);

            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, _plainNameRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
            Assert.Equal(MemberEndKind.Left, leftRow.EndKind);
            Assert.Null(_plainNameRow.UserId);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_APersonWithASetAsideRowAndNoCurrentRowWhoSendsAClaim_FailsWith400MemberCannotClaim()
        {
            _members.Seed(MemberTestData.SetAside(_group.Id, GroupTestData.StrangerId, JoinerName, GroupTestData.Moment.AddDays(-1)));

            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, _plainNameRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_APersonWhoLeftAndSendsAnUnknownClaimTarget_FailsWith404BeforeTheirOwnClaimRule()
        {
            _members.Seed(MemberTestData.Left(_group.Id, GroupTestData.StrangerId, JoinerName));

            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, Guid.NewGuid());

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        private Task<Result<Guid>> ExecuteAsync(Guid userId, Guid? claimMemberId)
        {
            return _joinGroup.Execute(GroupTestData.InviteToken, userId, JoinerName, claimMemberId, TestContext.Current.CancellationToken);
        }

        private void AssertNothingWritten()
        {
            Assert.Empty(_members.Added);
            Assert.Empty(_activityEvents.Added);
            Assert.Empty(_usageEvents.Added);
        }
    }
}
