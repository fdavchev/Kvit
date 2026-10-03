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
    public class DeleteGroupTests
    {
        private readonly FakeGroupRepository _groups = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly DeleteGroup _deleteGroup;
        private readonly Group _group = GroupTestData.NewGroup();

        public DeleteGroupTests()
        {
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _deleteGroup = new DeleteGroup(_groups, _activityEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_Owner_MarksTheGroupDeletedNowByTheOwner()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.Moment, _group.DeletedAt);
            Assert.Equal(GroupTestData.OwnerId, _group.DeletedByUserId);
        }

        [Fact]
        public async Task Execute_Owner_WritesOneGroupDeletedEvent()
        {
            await ExecuteAsync(GroupTestData.OwnerId);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.GroupDeleted, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
        }

        [Fact]
        public async Task Execute_NonMember_FailsWith404AndDeletesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.StrangerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Null(_group.DeletedAt);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_MemberWhoIsNotTheOwner_FailsWith403AndDeletesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Null(_group.DeletedAt);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_GroupThatIsAlreadyDeleted_FailsWith404()
        {
            _group.Delete(GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-1));

            Result result = await ExecuteAsync(GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Empty(_activityEvents.Added);
        }

        private Task<Result> ExecuteAsync(Guid userId)
        {
            return _deleteGroup.Execute(_group.Id, userId, TestContext.Current.CancellationToken);
        }
    }
}
