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
    public class ResetInviteLinkTests
    {
        private const string GeneratedToken = "token-from-the-generator";

        private readonly FakeGroupRepository _groups = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly ResetInviteLink _resetInviteLink;
        private readonly Group _group = GroupTestData.NewGroup();

        public ResetInviteLinkTests()
        {
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _resetInviteLink = new ResetInviteLink(_groups, _activityEvents, new FakeInviteTokenGenerator(GeneratedToken), new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_Owner_ReturnsTheNewTokenFromTheGenerator()
        {
            Result<string> result = await ExecuteAsync(GroupTestData.OwnerId);

            Assert.True(result.IsSuccess);
            Assert.Equal(GeneratedToken, result.Value);
            Assert.Equal(GeneratedToken, _group.InviteToken);
        }

        [Fact]
        public async Task Execute_Owner_KeepsTheOldTokenAsPreviousAndStampsTheMoment()
        {
            await ExecuteAsync(GroupTestData.OwnerId);

            Assert.Equal(GroupTestData.InviteToken, _group.PreviousInviteToken);
            Assert.Equal(GroupTestData.Moment, _group.InviteTokenCreatedAt);
        }

        [Fact]
        public async Task Execute_Owner_WritesOneInviteLinkResetEventThatCarriesNoToken()
        {
            await ExecuteAsync(GroupTestData.OwnerId);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.InviteLinkReset, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Null(activityEvent.Data);
            Assert.Null(activityEvent.Changes);
        }

        [Fact]
        public async Task Execute_ANonMember_FailsWith404GroupNotFoundAndChangesNothing()
        {
            Result<string> result = await ExecuteAsync(GroupTestData.StrangerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertTokenUnchanged();
        }

        [Fact]
        public async Task Execute_ADeletedGroup_FailsWith404GroupNotFound()
        {
            _group.Delete(GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-1));

            Result<string> result = await ExecuteAsync(GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertTokenUnchanged();
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwner_FailsWith403GroupNotOwnerAndChangesNothing()
        {
            Result<string> result = await ExecuteAsync(GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            AssertTokenUnchanged();
        }

        private Task<Result<string>> ExecuteAsync(Guid userId)
        {
            return _resetInviteLink.Execute(_group.Id, userId, TestContext.Current.CancellationToken);
        }

        private void AssertTokenUnchanged()
        {
            Assert.Equal(GroupTestData.InviteToken, _group.InviteToken);
            Assert.Null(_group.PreviousInviteToken);
            Assert.Empty(_activityEvents.Added);
        }
    }
}
