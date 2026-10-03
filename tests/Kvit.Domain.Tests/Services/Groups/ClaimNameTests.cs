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
    public class ClaimNameTests
    {
        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly ClaimName _claimName;
        private readonly Group _group = GroupTestData.NewGroup();
        private readonly GroupMember _ownerRow;
        private readonly GroupMember _memberRow;
        private readonly GroupMember _plainNameRow;

        public ClaimNameTests()
        {
            _ownerRow = MemberTestData.Account(_group.Id, GroupTestData.OwnerId, "Filip");
            _memberRow = MemberTestData.Account(_group.Id, GroupTestData.MemberId, "Ana");
            _plainNameRow = MemberTestData.PlainName(_group.Id, "Grandma");
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _members.Seed(_ownerRow);
            _members.Seed(_memberRow, "Ana Petrova");
            _members.Seed(_plainNameRow);
            _claimName = new ClaimName(_groups, _members, _activityEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_AMemberClaimsAPlainName_SetsTheirOwnRowAsideAndPutsTheirAccountOnTheName()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, _plainNameRow.Id);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.Moment, _memberRow.RemovedAt);
            Assert.Equal(GroupTestData.MemberId, _memberRow.RemovedByUserId);
            Assert.Equal(MemberEndKind.SetAside, _memberRow.EndKind);
            Assert.Equal(GroupTestData.MemberId, _plainNameRow.UserId);
            Assert.Equal(GroupTestData.Moment, _plainNameRow.ClaimedAt);
            Assert.Equal("Grandma", _plainNameRow.Name);
            Assert.Null(_plainNameRow.RemovedAt);
        }

        [Fact]
        public async Task Execute_AMemberClaimsAPlainName_AddsNoNewRow()
        {
            await ExecuteAsync(GroupTestData.MemberId, _plainNameRow.Id);

            Assert.Empty(_members.Added);
        }

        [Fact]
        public async Task Execute_AMemberClaimsAPlainName_SavesTheSetAsideBeforeTheNameIsClaimed()
        {
            await ExecuteAsync(GroupTestData.MemberId, _plainNameRow.Id);

            IReadOnlyList<MemberSnapshot> savedState = Assert.Single(_members.SavedStates);
            MemberSnapshot ownRowWhenSaved = Assert.Single(savedState, row => row.Id == _memberRow.Id);
            MemberSnapshot targetWhenSaved = Assert.Single(savedState, row => row.Id == _plainNameRow.Id);
            Assert.Equal(MemberEndKind.SetAside, ownRowWhenSaved.EndKind);
            Assert.Equal(GroupTestData.Moment, ownRowWhenSaved.RemovedAt);
            Assert.Null(targetWhenSaved.UserId);
            Assert.Null(targetWhenSaved.ClaimedAt);
        }

        [Fact]
        public async Task Execute_AMemberClaimsAPlainName_WritesOneMemberClaimedEventWithTheClaimersDisplayedNameAndTheClaimedName()
        {
            await ExecuteAsync(GroupTestData.MemberId, _plainNameRow.Id);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.MemberClaimed, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.MemberId, activityEvent.ActorUserId);
            Assert.Equal(_plainNameRow.Id, activityEvent.MemberId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal("Ana Petrova", ActivityEventData.Name(activityEvent));
            Assert.Equal("Grandma", ActivityEventData.ClaimedName(activityEvent));
        }

        [Fact]
        public async Task Execute_TheOwnerClaimsAName_SucceedsAndStaysTheOwner()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, _plainNameRow.Id);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.OwnerId, _group.OwnerUserId);
            Assert.Equal(MemberEndKind.SetAside, _ownerRow.EndKind);
            Assert.Equal(GroupTestData.OwnerId, _plainNameRow.UserId);
        }

        [Fact]
        public async Task Execute_ANonMember_FailsWith404GroupNotFoundAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.StrangerId, _plainNameRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertNothingChanged();
        }

        [Fact]
        public async Task Execute_ADeletedGroup_FailsWith404GroupNotFound()
        {
            _group.Delete(GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-1));

            Result result = await ExecuteAsync(GroupTestData.MemberId, _plainNameRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertNothingChanged();
        }

        [Fact]
        public async Task Execute_AnUnknownTarget_FailsWith404MemberNotFoundAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, Guid.NewGuid());

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            AssertNothingChanged();
        }

        [Fact]
        public async Task Execute_ARowOfAnotherGroup_FailsWith404MemberNotFound()
        {
            GroupMember otherGroupRow = MemberTestData.PlainName(Guid.NewGuid(), "Stranger");
            _members.Seed(otherGroupRow);

            Result result = await ExecuteAsync(GroupTestData.MemberId, otherGroupRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Null(otherGroupRow.UserId);
        }

        [Fact]
        public async Task Execute_ARemovedPlainName_FailsWith404MemberNotFound()
        {
            GroupMember removed = MemberTestData.RemovedPlainName(_group.Id, "Aunt");
            _members.Seed(removed);

            Result result = await ExecuteAsync(GroupTestData.MemberId, removed.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            AssertNothingChanged();
        }

        [Fact]
        public async Task Execute_AnAccountRowAsTheTarget_FailsWith404MemberNotFound()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, _ownerRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            AssertNothingChanged();
        }

        [Fact]
        public async Task Execute_ANameThatIsAlreadyClaimed_FailsWith404MemberNotFound()
        {
            GroupMember claimed = MemberTestData.Claimed(_group.Id, MemberTestData.OtherUserId, "Marko");
            _members.Seed(claimed);

            Result result = await ExecuteAsync(GroupTestData.MemberId, claimed.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(MemberTestData.OtherUserId, claimed.UserId);
            AssertNothingChanged();
        }

        [Fact]
        public async Task Execute_AMemberWhoAlreadyHoldsAClaimedName_FailsWith400MemberCannotClaimAndSavesNothing()
        {
            GroupMember alreadyClaimed = MemberTestData.Claimed(_group.Id, GroupTestData.MemberId, "Marko");
            _members.Seed(alreadyClaimed);
            _memberRow.SetAside(GroupTestData.Moment.AddDays(-1));

            Result result = await ExecuteAsync(GroupTestData.MemberId, _plainNameRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
            Assert.Null(_plainNameRow.UserId);
            Assert.Null(alreadyClaimed.RemovedAt);
            Assert.Empty(_members.SavedStates);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_AMemberHoldingAClaimedNameAndAnUnknownTarget_FailsWith404BeforeTheMembersOwnRuleIsChecked()
        {
            GroupMember alreadyClaimed = MemberTestData.Claimed(_group.Id, GroupTestData.MemberId, "Marko");
            _members.Seed(alreadyClaimed);
            _memberRow.SetAside(GroupTestData.Moment.AddDays(-1));

            Result result = await ExecuteAsync(GroupTestData.MemberId, Guid.NewGuid());

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Execute_AMemberInTheGroupWithNoCurrentRow_ThrowsAnErrorNamingTheUserAndTheGroup()
        {
            _memberRow.SetAside(GroupTestData.Moment.AddDays(-1));

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteAsync(GroupTestData.MemberId, _plainNameRow.Id));

            Assert.Contains(GroupTestData.MemberId.ToString(), exception.Message, StringComparison.Ordinal);
            Assert.Contains(_group.Id.ToString(), exception.Message, StringComparison.Ordinal);
        }

        private Task<Result> ExecuteAsync(Guid userId, Guid memberId)
        {
            return _claimName.Execute(_group.Id, userId, memberId, TestContext.Current.CancellationToken);
        }

        private void AssertNothingChanged()
        {
            Assert.Null(_memberRow.RemovedAt);
            Assert.Null(_ownerRow.RemovedAt);
            Assert.Null(_plainNameRow.UserId);
            Assert.Null(_plainNameRow.ClaimedAt);
            Assert.Empty(_members.SavedStates);
            Assert.Empty(_activityEvents.Added);
        }
    }
}
