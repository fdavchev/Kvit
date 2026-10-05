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
    public class LetBackInTests
    {
        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly LetBackIn _letBackIn;
        private readonly Group _group = GroupTestData.NewGroup();
        private readonly GroupMember _removedAccount;
        private readonly GroupMember _removedPlainName;

        public LetBackInTests()
        {
            _removedAccount = MemberTestData.Removed(_group.Id, MemberTestData.OtherUserId, "Marko");
            _removedPlainName = MemberTestData.RemovedPlainName(_group.Id, "Grandma");
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _members.Seed(MemberTestData.Account(_group.Id, GroupTestData.OwnerId, "Filip"));
            _members.Seed(MemberTestData.Account(_group.Id, GroupTestData.MemberId, "Ana"));
            _members.Seed(_removedAccount, "Marko Petrov");
            _members.Seed(_removedPlainName);
            _letBackIn = new LetBackIn(_groups, _members, _activityEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Execute_OwnerLetsBackARemovedRow_MakesItCurrentAgain(bool isAccount)
        {
            GroupMember target = isAccount ? _removedAccount : _removedPlainName;

            Result result = await ExecuteAsync(GroupTestData.OwnerId, target.Id);

            Assert.True(result.IsSuccess);
            Assert.Null(target.RemovedAt);
            Assert.Null(target.RemovedByUserId);
            Assert.Null(target.EndKind);
        }

        [Fact]
        public async Task Execute_OwnerLetsBackARemovedAccount_WritesOneMemberLetBackInEventWithTheDisplayedName()
        {
            await ExecuteAsync(GroupTestData.OwnerId, _removedAccount.Id);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.MemberLetBackIn, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(_removedAccount.Id, activityEvent.MemberId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal("Marko Petrov", ActivityEventData.Name(activityEvent));
        }

        [Fact]
        public async Task Execute_ARemovedPlainNameWhoseNameIsNowTaken_FailsWith400MemberNameTakenAndStaysRemoved()
        {
            _members.Seed(MemberTestData.PlainName(_group.Id, "grandma"));

            Result result = await ExecuteAsync(GroupTestData.OwnerId, _removedPlainName.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
            Assert.Equal(MemberEndKind.Removed, _removedPlainName.EndKind);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_ARemovedPlainNameWhoseNameIsTakenOnlyByAnotherRemovedRow_IsLetBackIn()
        {
            _members.Seed(MemberTestData.RemovedPlainName(_group.Id, "Grandma"));

            Result result = await ExecuteAsync(GroupTestData.OwnerId, _removedPlainName.Id);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task Execute_ARemovedAccountWhoseNameMatchesACurrentMember_IsStillLetBackIn()
        {
            _members.Seed(MemberTestData.PlainName(_group.Id, "Marko Petrov"));

            Result result = await ExecuteAsync(GroupTestData.OwnerId, _removedAccount.Id);

            Assert.True(result.IsSuccess);
            Assert.Null(_removedAccount.EndKind);
        }

        [Fact]
        public async Task Execute_ANonMember_FailsWith404GroupNotFoundAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.StrangerId, _removedAccount.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertStillRemoved();
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwner_FailsWith403GroupNotOwnerAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, _removedAccount.Id);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            AssertStillRemoved();
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
            AssertStillRemoved();
        }

        [Fact]
        public async Task Execute_ACurrentMember_FailsWith404MemberNotFound()
        {
            GroupMember current = MemberTestData.PlainName(_group.Id, "Bojan");
            _members.Seed(current);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, current.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_APersonWhoLeftOnTheirOwn_FailsWith404MemberNotFoundBecauseOnlyRemovedRowsComeBack()
        {
            GroupMember leftRow = MemberTestData.Left(_group.Id, GroupTestData.StrangerId, "Bojan");
            _members.Seed(leftRow);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, leftRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(MemberEndKind.Left, leftRow.EndKind);
        }

        [Fact]
        public async Task Execute_ARowThatWasSetAside_FailsWith404MemberNotFound()
        {
            GroupMember setAside = MemberTestData.SetAside(_group.Id, GroupTestData.StrangerId, "Bojan", GroupTestData.Moment.AddDays(-1));
            _members.Seed(setAside);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, setAside.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(MemberEndKind.SetAside, setAside.EndKind);
        }

        private Task<Result> ExecuteAsync(Guid userId, Guid memberId)
        {
            return _letBackIn.Execute(_group.Id, userId, memberId, TestContext.Current.CancellationToken);
        }

        private void AssertStillRemoved()
        {
            Assert.Equal(MemberEndKind.Removed, _removedAccount.EndKind);
            Assert.Equal(MemberEndKind.Removed, _removedPlainName.EndKind);
            Assert.Empty(_activityEvents.Added);
        }
    }
}
