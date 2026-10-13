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
    public class ObjectToClosingTests
    {
        private static readonly DateTimeOffset ClosingStart = GroupTestData.Moment.AddHours(-1);

        private readonly FakeGroupRepository _groups = new();
        private readonly FakeClosingResponseRepository _closingResponses = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly ObjectToClosing _objectToClosing;
        private readonly Group _group = GroupTestData.NewClosingGroup(ClosingStart);
        private readonly Group _otherGroup = GroupTestData.NewClosingGroup(ClosingStart);

        public ObjectToClosingTests()
        {
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId, MemberTestData.OtherUserId);
            _closingResponses.Seed(ClosingResponse.Confirmed(_group.Id, Guid.CreateVersion7(), ClosingStart));
            _closingResponses.Seed(ClosingResponse.Confirmed(_otherGroup.Id, Guid.CreateVersion7(), ClosingStart));
            _objectToClosing = new ObjectToClosing(
                _groups,
                _closingResponses,
                _activityEvents,
                new FinishExpiredClosing(_activityEvents),
                new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_Member_ReturnsTheGroupToOpenAndClearsTheClosingTime()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, "Not yet");

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupStatus.Open, _group.Status);
            Assert.Null(_group.ClosingStartedAt);
            Assert.Null(_group.FinishedAt);
        }

        [Fact]
        public async Task Execute_Member_ClearsEveryResponseOfThisGroupOnly()
        {
            await ExecuteAsync(GroupTestData.MemberId, "Not yet");

            ClosingResponse kept = Assert.Single(_closingResponses.Stored);
            Assert.Equal(_otherGroup.Id, kept.GroupId);
        }

        [Fact]
        public async Task Execute_MemberWithAReasonWithSpaces_WritesOneObjectedEventWithTheTrimmedReason()
        {
            await ExecuteAsync(GroupTestData.MemberId, "  I still owe Marko 200  ");

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.ClosingObjected, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.MemberId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal("I still owe Marko 200", ActivityEventData.Reason(activityEvent));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Execute_MemberWithoutAReason_WritesTheEventWithNoReason(string? reason)
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, reason);

            Assert.True(result.IsSuccess);
            Assert.Null(ActivityEventData.Reason(Assert.Single(_activityEvents.Added)));
        }

        [Fact]
        public async Task Execute_ReasonOf200Characters_IsAccepted()
        {
            string reason = new('а', ClosingResponse.ReasonMaxLength);

            Result result = await ExecuteAsync(GroupTestData.MemberId, reason);

            Assert.True(result.IsSuccess);
            Assert.Equal(reason, ActivityEventData.Reason(Assert.Single(_activityEvents.Added)));
        }

        [Fact]
        public async Task Execute_ReasonOf201Characters_FailsWith400ClosingReasonTooLongAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, new string('x', ClosingResponse.ReasonMaxLength + 1));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CLOSING_REASON_TOO_LONG);
            Assert.Equal(GroupStatus.Closing, _group.Status);
            Assert.Equal(ClosingStart, _group.ClosingStartedAt);
            Assert.Equal(2, _closingResponses.Stored.Count);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_Owner_FailsWith400ClosingOwnerCannotAnswerAndKeepsTheGroupClosing()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, "I changed my mind");

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CLOSING_OWNER_CANNOT_ANSWER);
            Assert.Equal(GroupStatus.Closing, _group.Status);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_Stranger_FailsWith404()
        {
            Result result = await ExecuteAsync(GroupTestData.StrangerId, null);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(GroupStatus.Closing, _group.Status);
        }

        [Fact]
        public async Task Execute_GroupIsOpen_FailsWith400GroupNotClosing()
        {
            _group.CancelClosing();

            Result result = await ExecuteAsync(GroupTestData.MemberId, "No");

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_CLOSING);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_GroupIsFinished_FailsWith400GroupNotClosingAndKeepsItFinished()
        {
            _group.Finish(ClosingStart.AddMinutes(5));

            Result result = await ExecuteAsync(GroupTestData.MemberId, "Too late");

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_CLOSING);
            Assert.Equal(GroupStatus.Finished, _group.Status);
            Assert.Equal(2, _closingResponses.Stored.Count);
        }

        private Task<Result> ExecuteAsync(Guid userId, string? reason)
        {
            return _objectToClosing.Execute(_group.Id, userId, reason, TestContext.Current.CancellationToken);
        }
    }
}
