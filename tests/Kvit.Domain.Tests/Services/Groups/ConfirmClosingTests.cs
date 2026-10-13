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
    public class ConfirmClosingTests
    {
        private static readonly Guid ThirdUserId = MemberTestData.OtherUserId;
        private static readonly DateTimeOffset ClosingStart = GroupTestData.Moment.AddHours(-1);

        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly FakeClosingResponseRepository _closingResponses = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly Group _group;
        private readonly GroupMember _memberRow;
        private readonly GroupMember _thirdRow;

        public ConfirmClosingTests()
        {
            _group = GroupTestData.NewClosingGroup(ClosingStart);
            _memberRow = MemberTestData.Account(_group.Id, GroupTestData.MemberId, "Ana");
            _thirdRow = MemberTestData.Account(_group.Id, ThirdUserId, "Dime");
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId, ThirdUserId);
            _members.Seed(MemberTestData.Account(_group.Id, GroupTestData.OwnerId, "Filip"));
            _members.Seed(_memberRow);
            _members.Seed(_thirdRow);
            _members.Seed(MemberTestData.PlainName(_group.Id, "Grandma"));
        }

        [Fact]
        public async Task Execute_MemberWhileAnotherIsStillWaiting_StoresAConfirmedRowNowAndKeepsTheGroupClosing()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId);

            Assert.True(result.IsSuccess);
            ClosingResponse response = Assert.Single(_closingResponses.Stored);
            Assert.Equal(_group.Id, response.GroupId);
            Assert.Equal(_memberRow.Id, response.MemberId);
            Assert.Equal(ClosingAnswer.Confirmed, response.Response);
            Assert.Null(response.Reason);
            Assert.Equal(GroupTestData.Moment, response.RespondedAt);
            Assert.Equal(GroupStatus.Closing, _group.Status);
            Assert.Null(_group.FinishedAt);
        }

        [Fact]
        public async Task Execute_MemberWhileAnotherIsStillWaiting_WritesOnlyAClosingConfirmedEventByThatMember()
        {
            await ExecuteAsync(GroupTestData.MemberId);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.ClosingConfirmed, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.MemberId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
        }

        [Fact]
        public async Task Execute_LastWaitingMember_FinishesTheGroupNowAndWritesConfirmedThenFinishedByThatMember()
        {
            _closingResponses.Seed(ClosingResponse.Confirmed(_group.Id, _memberRow.Id, ClosingStart));

            Result result = await ExecuteAsync(ThirdUserId);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupStatus.Finished, _group.Status);
            Assert.Equal(GroupTestData.Moment, _group.FinishedAt);
            Assert.Equal(2, _closingResponses.Stored.Count);
            Assert.Equal([ActivityEventType.ClosingConfirmed, ActivityEventType.GroupFinished], _activityEvents.Added.Select(activityEvent => activityEvent.Type));
            Assert.All(_activityEvents.Added, activityEvent => Assert.Equal(ThirdUserId, activityEvent.ActorUserId));
        }

        [Fact]
        public async Task Execute_WhenTheOtherWaitingMemberLeft_TheOneConfirmationFinishesTheGroup()
        {
            _thirdRow.Leave(GroupTestData.OwnerId, ClosingStart.AddMinutes(1));

            await ExecuteAsync(GroupTestData.MemberId);

            Assert.Equal(GroupStatus.Finished, _group.Status);
        }

        [Fact]
        public async Task Execute_Owner_FailsWith400ClosingOwnerCannotAnswerAndStoresNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CLOSING_OWNER_CANNOT_ANSWER);
            Assert.Empty(_closingResponses.Stored);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_OwnerWhileTheGroupIsOpen_FailsWith400ClosingOwnerCannotAnswerBeforeTheStateIsChecked()
        {
            _group.CancelClosing();

            Result result = await ExecuteAsync(GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CLOSING_OWNER_CANNOT_ANSWER);
        }

        [Fact]
        public async Task Execute_Stranger_FailsWith404()
        {
            Result result = await ExecuteAsync(GroupTestData.StrangerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Empty(_closingResponses.Stored);
        }

        [Fact]
        public async Task Execute_GroupIsOpen_FailsWith400GroupNotClosing()
        {
            _group.CancelClosing();

            Result result = await ExecuteAsync(GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_CLOSING);
            Assert.Empty(_closingResponses.Stored);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_GroupIsFinished_FailsWith400GroupNotClosing()
        {
            _group.Finish(ClosingStart.AddMinutes(5));

            Result result = await ExecuteAsync(GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_CLOSING);
            Assert.Empty(_closingResponses.Stored);
        }

        [Fact]
        public async Task Execute_MemberWhoAlreadyConfirmed_FailsWith400ClosingAlreadyAnsweredAndKeepsTheFirstRow()
        {
            _closingResponses.Seed(ClosingResponse.Confirmed(_group.Id, _memberRow.Id, ClosingStart));

            Result result = await ExecuteAsync(GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CLOSING_ALREADY_ANSWERED);
            ClosingResponse response = Assert.Single(_closingResponses.Stored);
            Assert.Equal(ClosingStart, response.RespondedAt);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_After24Hours_FailsWith400GroupNotClosingAndStoresNoResponse()
        {
            Group expired = GroupTestData.NewClosingGroup(GroupTestData.Moment.AddHours(-Group.ClosingFinishHours));
            _groups.Seed(expired, GroupTestData.OwnerId, GroupTestData.MemberId);
            _members.Seed(MemberTestData.Account(expired.Id, GroupTestData.MemberId, "Ana"));

            Result result = await ExecuteAsync(GroupTestData.MemberId, expired.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_CLOSING);
            Assert.Empty(_closingResponses.Stored);
        }

        private Task<Result> ExecuteAsync(Guid userId)
        {
            return ExecuteAsync(userId, _group.Id);
        }

        private Task<Result> ExecuteAsync(Guid userId, Guid groupId)
        {
            ConfirmClosing confirmClosing = new(
                _groups,
                _members,
                _closingResponses,
                _activityEvents,
                new FinishExpiredClosing(_activityEvents),
                new FixedTimeProvider(GroupTestData.Moment));

            return confirmClosing.Execute(groupId, userId, TestContext.Current.CancellationToken);
        }
    }
}
