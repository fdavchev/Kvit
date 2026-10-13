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
    public class UnlockGroupTests
    {
        private static readonly DateTimeOffset ClosingStart = GroupTestData.Moment.AddHours(-3);

        private readonly FakeGroupRepository _groups = new();
        private readonly FakeClosingResponseRepository _closingResponses = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly UnlockGroup _unlockGroup;

        public UnlockGroupTests()
        {
            _unlockGroup = new UnlockGroup(
                _groups,
                _closingResponses,
                _activityEvents,
                new FinishExpiredClosing(_activityEvents),
                new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_OwnerOfAFinishedGroup_ReturnsItToOpenAndClearsBothTimes()
        {
            Group group = Seed(GroupTestData.NewFinishedGroup(ClosingStart, ClosingStart.AddHours(1)));

            Result result = await ExecuteAsync(group, GroupTestData.OwnerId);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupStatus.Open, group.Status);
            Assert.Null(group.ClosingStartedAt);
            Assert.Null(group.FinishedAt);
        }

        [Fact]
        public async Task Execute_OwnerOfAFinishedGroup_ClearsTheResponsesAndWritesOneGroupReopenedEvent()
        {
            Group group = Seed(GroupTestData.NewFinishedGroup(ClosingStart, ClosingStart.AddHours(1)));
            _closingResponses.Seed(ClosingResponse.Confirmed(group.Id, Guid.CreateVersion7(), ClosingStart.AddHours(1)));

            await ExecuteAsync(group, GroupTestData.OwnerId);

            Assert.Empty(_closingResponses.Stored);
            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.GroupReopened, activityEvent.Type);
            Assert.Equal(group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
        }

        [Fact]
        public async Task Execute_MemberWhoIsNotTheOwner_FailsWith403AndKeepsTheGroupFinished()
        {
            Group group = Seed(GroupTestData.NewFinishedGroup(ClosingStart, ClosingStart.AddHours(1)));

            Result result = await ExecuteAsync(group, GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(GroupStatus.Finished, group.Status);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_MemberWhoIsNotTheOwnerOfAnOpenGroup_FailsWith403BeforeTheStateIsChecked()
        {
            Group group = Seed(GroupTestData.NewGroup());

            Result result = await ExecuteAsync(group, GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Execute_Stranger_FailsWith404()
        {
            Group group = Seed(GroupTestData.NewFinishedGroup(ClosingStart, ClosingStart.AddHours(1)));

            Result result = await ExecuteAsync(group, GroupTestData.StrangerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(GroupStatus.Finished, group.Status);
        }

        [Fact]
        public async Task Execute_OpenGroup_FailsWith400GroupNotFinished()
        {
            Group group = Seed(GroupTestData.NewGroup());

            Result result = await ExecuteAsync(group, GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_FINISHED);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_ClosingInsideThe24Hours_FailsWith400GroupNotFinishedAndKeepsItClosing()
        {
            Group group = Seed(GroupTestData.NewClosingGroup(ClosingStart));
            _closingResponses.Seed(ClosingResponse.Confirmed(group.Id, Guid.CreateVersion7(), ClosingStart));

            Result result = await ExecuteAsync(group, GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_FINISHED);
            Assert.Equal(GroupStatus.Closing, group.Status);
            Assert.Single(_closingResponses.Stored);
        }

        [Fact]
        public async Task Execute_ClosingThatRanOut_SavesTheFinishAtTheEndOfThe24HoursAndThenReopens()
        {
            DateTimeOffset expiredStart = GroupTestData.Moment.AddHours(-Group.ClosingFinishHours - 2);
            Group group = Seed(GroupTestData.NewClosingGroup(expiredStart));

            Result result = await ExecuteAsync(group, GroupTestData.OwnerId);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupStatus.Open, group.Status);
            Assert.Equal([ActivityEventType.GroupFinished, ActivityEventType.GroupReopened], _activityEvents.Added.Select(activityEvent => activityEvent.Type));
            Assert.Equal(expiredStart.AddHours(Group.ClosingFinishHours), _activityEvents.Added[0].CreatedAt);
            Assert.Equal(GroupTestData.Moment, _activityEvents.Added[1].CreatedAt);
        }

        private Group Seed(Group group)
        {
            _groups.Seed(group, GroupTestData.OwnerId, GroupTestData.MemberId);

            return group;
        }

        private Task<Result> ExecuteAsync(Group group, Guid userId)
        {
            return _unlockGroup.Execute(group.Id, userId, TestContext.Current.CancellationToken);
        }
    }
}
