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
    public class MakeOwnerTests
    {
        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly MakeOwner _makeOwner;
        private readonly Group _group = GroupTestData.NewGroup();
        private readonly GroupMember _ownerRow;
        private readonly GroupMember _memberRow;
        private readonly GroupMember _plainNameRow;

        public MakeOwnerTests()
        {
            _ownerRow = MemberTestData.Account(_group.Id, GroupTestData.OwnerId, "Filip");
            _memberRow = MemberTestData.Account(_group.Id, GroupTestData.MemberId, "Ana");
            _plainNameRow = MemberTestData.PlainName(_group.Id, "Grandma");
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _members.Seed(_ownerRow);
            _members.Seed(_memberRow, "Ana Petrova");
            _members.Seed(_plainNameRow);
            _makeOwner = new MakeOwner(_groups, _members, _activityEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_OwnerPicksAnAccount_MakesThatAccountTheOwnerAndKeepsTheOldOwnerAsAMember()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, _memberRow.Id);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.MemberId, _group.OwnerUserId);
            Assert.Null(_ownerRow.RemovedAt);
            Assert.Null(_memberRow.RemovedAt);
        }

        [Fact]
        public async Task Execute_OwnerPicksAnAccount_WritesOneOwnershipTransferredEventForTheNewOwnersRow()
        {
            await ExecuteAsync(GroupTestData.OwnerId, _memberRow.Id);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.OwnershipTransferred, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(_memberRow.Id, activityEvent.MemberId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal("Ana Petrova", ActivityEventData.Name(activityEvent));
        }

        [Fact]
        public async Task Execute_ANonMember_FailsWith404GroupNotFoundAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.StrangerId, _memberRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertOwnerUnchanged();
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwner_FailsWith403GroupNotOwnerAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, _memberRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            AssertOwnerUnchanged();
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwnerAndAPlainNameTarget_FailsWith403BeforeTheMemberRule()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, _plainNameRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Execute_AnUnknownTarget_FailsWith404MemberNotFound()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, Guid.NewGuid());

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            AssertOwnerUnchanged();
        }

        [Fact]
        public async Task Execute_ARemovedMember_FailsWith404MemberNotFound()
        {
            GroupMember removed = MemberTestData.Removed(_group.Id, MemberTestData.OtherUserId, "Marko");
            _members.Seed(removed);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, removed.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            AssertOwnerUnchanged();
        }

        [Fact]
        public async Task Execute_APersonWhoLeft_FailsWith404MemberNotFound()
        {
            GroupMember leftRow = MemberTestData.Left(_group.Id, MemberTestData.OtherUserId, "Marko");
            _members.Seed(leftRow);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, leftRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            AssertOwnerUnchanged();
        }

        [Fact]
        public async Task Execute_APlainName_FailsWith400MemberNotAccount()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, _plainNameRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_ACCOUNT);
            AssertOwnerUnchanged();
        }

        [Fact]
        public async Task Execute_TheOwnerThemselves_FailsWith400MemberAlreadyOwner()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, _ownerRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_ALREADY_OWNER);
            AssertOwnerUnchanged();
        }

        private Task<Result> ExecuteAsync(Guid userId, Guid memberId)
        {
            return _makeOwner.Execute(_group.Id, userId, memberId, TestContext.Current.CancellationToken);
        }

        private void AssertOwnerUnchanged()
        {
            Assert.Equal(GroupTestData.OwnerId, _group.OwnerUserId);
            Assert.Empty(_activityEvents.Added);
        }
    }
}
