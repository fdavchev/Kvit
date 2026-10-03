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
    public class LeaveGroupTests
    {
        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly LeaveGroup _leaveGroup;
        private readonly Group _group = GroupTestData.NewGroup();
        private readonly GroupMember _ownerRow;
        private readonly GroupMember _memberRow;

        public LeaveGroupTests()
        {
            _ownerRow = MemberTestData.Account(_group.Id, GroupTestData.OwnerId, "Filip");
            _memberRow = MemberTestData.Account(_group.Id, GroupTestData.MemberId, "Ana");
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _members.Seed(_ownerRow);
            _members.Seed(_memberRow, "Ana Petrova");
            _leaveGroup = new LeaveGroup(_groups, _members, _activityEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwner_EndsTheirRowAsLeftByThemselvesNow()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.Moment, _memberRow.RemovedAt);
            Assert.Equal(GroupTestData.MemberId, _memberRow.RemovedByUserId);
            Assert.Equal(MemberEndKind.Left, _memberRow.EndKind);
            Assert.Null(_ownerRow.RemovedAt);
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwner_WritesOneMemberLeftEventWithTheDisplayedName()
        {
            await ExecuteAsync(GroupTestData.MemberId);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.MemberLeft, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.MemberId, activityEvent.ActorUserId);
            Assert.Equal(_memberRow.Id, activityEvent.MemberId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal("Ana Petrova", ActivityEventData.Name(activityEvent));
        }

        [Fact]
        public async Task Execute_AMemberHoldingAClaimedName_LeavesOnTheClaimedRowAndLeavesTheSetAsideRowAlone()
        {
            GroupMember setAside = MemberTestData.SetAside(_group.Id, MemberTestData.OtherUserId, "Marko", GroupTestData.Moment.AddDays(-1));
            GroupMember claimed = MemberTestData.Claimed(_group.Id, MemberTestData.OtherUserId, "Grandma");
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId, MemberTestData.OtherUserId);
            _members.Seed(setAside);
            _members.Seed(claimed);

            Result result = await ExecuteAsync(MemberTestData.OtherUserId);

            Assert.True(result.IsSuccess);
            Assert.Equal(MemberEndKind.Left, claimed.EndKind);
            Assert.Equal(MemberEndKind.SetAside, setAside.EndKind);
            Assert.Equal(claimed.Id, Assert.Single(_activityEvents.Added).MemberId);
        }

        [Fact]
        public async Task Execute_TheOwner_FailsWith400MemberOwnerCannotLeaveAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_OWNER_CANNOT_LEAVE);
            Assert.Null(_ownerRow.RemovedAt);
            Assert.Null(_ownerRow.EndKind);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_ANonMember_FailsWith404GroupNotFoundAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.StrangerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Null(_memberRow.RemovedAt);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_ADeletedGroup_FailsWith404GroupNotFound()
        {
            _group.Delete(GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-1));

            Result result = await ExecuteAsync(GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Null(_memberRow.RemovedAt);
        }

        [Fact]
        public async Task Execute_AMemberWhoIsInTheGroupButHasNoCurrentRow_ThrowsAnErrorNamingTheUserAndTheGroup()
        {
            GroupMember leftRow = MemberTestData.Left(_group.Id, GroupTestData.StrangerId, "Bojan");
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId, GroupTestData.StrangerId);
            _members.Seed(leftRow);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteAsync(GroupTestData.StrangerId));

            Assert.Contains(GroupTestData.StrangerId.ToString(), exception.Message, StringComparison.Ordinal);
            Assert.Contains(_group.Id.ToString(), exception.Message, StringComparison.Ordinal);
        }

        private Task<Result> ExecuteAsync(Guid userId)
        {
            return _leaveGroup.Execute(_group.Id, userId, TestContext.Current.CancellationToken);
        }
    }
}
