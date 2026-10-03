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
    public class RemoveMemberTests
    {
        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly RemoveMember _removeMember;
        private readonly Group _group = GroupTestData.NewGroup();
        private readonly GroupMember _ownerRow;
        private readonly GroupMember _accountRow;
        private readonly GroupMember _plainNameRow;

        public RemoveMemberTests()
        {
            _ownerRow = MemberTestData.Account(_group.Id, GroupTestData.OwnerId, "Filip");
            _accountRow = MemberTestData.Account(_group.Id, GroupTestData.MemberId, "Ana");
            _plainNameRow = MemberTestData.PlainName(_group.Id, "Grandma");
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _members.Seed(_ownerRow);
            _members.Seed(_accountRow, "Ana Petrova");
            _members.Seed(_plainNameRow);
            _removeMember = new RemoveMember(_groups, _members, _activityEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_OwnerRemovesAnAccount_EndsTheRowAsRemovedByTheOwnerNow()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, _accountRow.Id);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.Moment, _accountRow.RemovedAt);
            Assert.Equal(GroupTestData.OwnerId, _accountRow.RemovedByUserId);
            Assert.Equal(MemberEndKind.Removed, _accountRow.EndKind);
            Assert.Null(_ownerRow.RemovedAt);
            Assert.Null(_plainNameRow.RemovedAt);
        }

        [Fact]
        public async Task Execute_OwnerRemovesAPlainName_EndsTheRowAsRemoved()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, _plainNameRow.Id);

            Assert.True(result.IsSuccess);
            Assert.Equal(MemberEndKind.Removed, _plainNameRow.EndKind);
        }

        [Fact]
        public async Task Execute_OwnerRemovesAMember_WritesOneMemberRemovedEventWithTheDisplayedName()
        {
            await ExecuteAsync(GroupTestData.OwnerId, _accountRow.Id);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.MemberRemoved, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(_accountRow.Id, activityEvent.MemberId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal("Ana Petrova", ActivityEventData.Name(activityEvent));
        }

        [Fact]
        public async Task Execute_ANonMember_FailsWith404GroupNotFoundAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.StrangerId, _accountRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertNothingChanged();
        }

        [Fact]
        public async Task Execute_ADeletedGroup_FailsWith404GroupNotFound()
        {
            _group.Delete(GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-1));

            Result result = await ExecuteAsync(GroupTestData.OwnerId, _accountRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertNothingChanged();
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwner_FailsWith403GroupNotOwnerAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, _plainNameRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            AssertNothingChanged();
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwnerAndAnUnknownTarget_FailsWith403BeforeTheTargetIsLookedUp()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, Guid.NewGuid());

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Execute_AnUnknownTarget_FailsWith404MemberNotFound()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, Guid.NewGuid());

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            AssertNothingChanged();
        }

        [Fact]
        public async Task Execute_ARowOfAnotherGroup_FailsWith404MemberNotFound()
        {
            GroupMember otherGroupRow = MemberTestData.PlainName(Guid.NewGuid(), "Stranger");
            _members.Seed(otherGroupRow);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, otherGroupRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Null(otherGroupRow.RemovedAt);
        }

        [Fact]
        public async Task Execute_ARowThatWasAlreadyRemoved_FailsWith404MemberNotFoundAndKeepsTheFirstRemoval()
        {
            await ExecuteAsync(GroupTestData.OwnerId, _plainNameRow.Id);
            _activityEvents.Added.Clear();

            Result result = await ExecuteAsync(GroupTestData.OwnerId, _plainNameRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Empty(_activityEvents.Added);
            Assert.Equal(GroupTestData.Moment, _plainNameRow.RemovedAt);
        }

        [Fact]
        public async Task Execute_ARowThatLeftOnItsOwn_FailsWith404MemberNotFound()
        {
            GroupMember leftRow = MemberTestData.Left(_group.Id, MemberTestData.OtherUserId, "Marko");
            _members.Seed(leftRow);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, leftRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(MemberEndKind.Left, leftRow.EndKind);
        }

        [Fact]
        public async Task Execute_TheOwnerRemovingThemselves_FailsWith400MemberIsOwnerAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, _ownerRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_IS_OWNER);
            AssertNothingChanged();
        }

        private Task<Result> ExecuteAsync(Guid userId, Guid memberId)
        {
            return _removeMember.Execute(_group.Id, userId, memberId, TestContext.Current.CancellationToken);
        }

        private void AssertNothingChanged()
        {
            Assert.Null(_ownerRow.RemovedAt);
            Assert.Null(_accountRow.RemovedAt);
            Assert.Null(_plainNameRow.RemovedAt);
            Assert.Empty(_activityEvents.Added);
        }
    }
}
