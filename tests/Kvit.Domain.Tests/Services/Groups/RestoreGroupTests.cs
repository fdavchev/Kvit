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
    public class RestoreGroupTests
    {
        private static readonly DateTimeOffset DeletedAt = GroupTestData.Moment.AddDays(-2);

        private readonly FakeGroupRepository _groups = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly RestoreGroup _restoreGroup;
        private readonly Group _group = GroupTestData.NewDeletedGroup(DeletedAt);

        public RestoreGroupTests()
        {
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _restoreGroup = new RestoreGroup(_groups, _activityEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_OwnerOfADeletedGroup_ClearsTheDeletion()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId);

            Assert.True(result.IsSuccess);
            Assert.Null(_group.DeletedAt);
            Assert.Null(_group.DeletedByUserId);
        }

        [Fact]
        public async Task Execute_OwnerOfADeletedGroup_WritesOneGroupRestoredEvent()
        {
            await ExecuteAsync(GroupTestData.OwnerId);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.GroupRestored, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
        }

        [Fact]
        public async Task Execute_NonMember_FailsWith404AndKeepsTheGroupDeleted()
        {
            Result result = await ExecuteAsync(GroupTestData.StrangerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(DeletedAt, _group.DeletedAt);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_MemberWhoIsNotTheOwner_FailsWith403AndKeepsTheGroupDeleted()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(DeletedAt, _group.DeletedAt);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_MemberWhoIsNotTheOwnerOfAnExpiredGroup_FailsWith403BeforeTheExpiryIsChecked()
        {
            Group expired = GroupTestData.NewDeletedGroup(GroupTestData.Moment.AddDays(-31));
            _groups.Seed(expired, GroupTestData.OwnerId, GroupTestData.MemberId);

            Result result = await _restoreGroup.Execute(expired.Id, GroupTestData.MemberId, TestContext.Current.CancellationToken);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Execute_GroupThatIsNotDeleted_FailsWithGroupNotDeletedAndWritesNoEvent()
        {
            Group open = GroupTestData.NewGroup();
            _groups.Seed(open, GroupTestData.OwnerId);

            Result result = await _restoreGroup.Execute(open.Id, GroupTestData.OwnerId, TestContext.Current.CancellationToken);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_DELETED);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_GroupDeleted31DaysAgo_FailsWithGroupRestoreExpiredKeepsItDeletedAndWritesNoEvent()
        {
            Group expired = GroupTestData.NewDeletedGroup(GroupTestData.Moment.AddDays(-31));
            _groups.Seed(expired, GroupTestData.OwnerId);

            Result result = await _restoreGroup.Execute(expired.Id, GroupTestData.OwnerId, TestContext.Current.CancellationToken);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_RESTORE_EXPIRED);
            Assert.NotNull(expired.DeletedAt);
            Assert.Empty(_activityEvents.Added);
        }

        private Task<Result> ExecuteAsync(Guid userId)
        {
            return _restoreGroup.Execute(_group.Id, userId, TestContext.Current.CancellationToken);
        }
    }
}
