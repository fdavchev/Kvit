using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;
using Kvit.Domain.Tests.Entities;
using Kvit.Domain.Tests.Fakes;
using Kvit.Domain.Tests.MoneyRules;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Services.Groups
{
    public class StartClosingTests
    {
        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly FakeGroupMoneyRepository _money = new();
        private readonly FakeActivityEventRepository _activityEvents = new();

        [Fact]
        public async Task Execute_OwnerWhenEveryoneIsKvit_StartsTheClosingNowAndWritesOneClosingStartedEvent()
        {
            Group group = SeedGroupWithMember(GroupTestData.NewGroup());

            Result result = await ExecuteAsync(group, GroupTestData.OwnerId);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupStatus.Closing, group.Status);
            Assert.Equal(GroupTestData.Moment, group.ClosingStartedAt);
            Assert.Null(group.FinishedAt);
            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.ClosingStarted, activityEvent.Type);
            Assert.Equal(group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
        }

        [Fact]
        public async Task Execute_OwnerWithOnlyPlainNames_FinishesTheGroupAtOnceAndWritesStartedThenFinished()
        {
            Group group = GroupTestData.NewGroup();
            _groups.Seed(group, GroupTestData.OwnerId);
            _members.Seed(MemberTestData.Account(group.Id, GroupTestData.OwnerId, "Filip"));
            _members.Seed(MemberTestData.PlainName(group.Id, "Grandma"));

            Result result = await ExecuteAsync(group, GroupTestData.OwnerId);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupStatus.Finished, group.Status);
            Assert.Equal(GroupTestData.Moment, group.ClosingStartedAt);
            Assert.Equal(GroupTestData.Moment, group.FinishedAt);
            Assert.Equal([ActivityEventType.ClosingStarted, ActivityEventType.GroupFinished], _activityEvents.Added.Select(activityEvent => activityEvent.Type));
            Assert.All(_activityEvents.Added, activityEvent => Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId));
        }

        [Fact]
        public async Task Execute_OwnerWhoseOnlyOtherAccountMemberLeft_FinishesTheGroupAtOnce()
        {
            Group group = GroupTestData.NewGroup();
            _groups.Seed(group, GroupTestData.OwnerId);
            _members.Seed(MemberTestData.Account(group.Id, GroupTestData.OwnerId, "Filip"));
            _members.Seed(MemberTestData.Left(group.Id, GroupTestData.MemberId, "Ana"));

            await ExecuteAsync(group, GroupTestData.OwnerId);

            Assert.Equal(GroupStatus.Finished, group.Status);
        }

        [Fact]
        public async Task Execute_MemberWhoIsNotTheOwner_FailsWith403BeforeTheMoneyIsAskedFor()
        {
            Group group = SeedGroupWithMember(GroupTestData.NewGroup());

            Result result = await ExecuteAsync(group, GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(GroupStatus.Open, group.Status);
            Assert.Empty(_money.RequestedGroupIds);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_Stranger_FailsWith404()
        {
            Group group = SeedGroupWithMember(GroupTestData.NewGroup());

            Result result = await ExecuteAsync(group, GroupTestData.StrangerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_OneBill_FailsWith400GroupIsOneBill()
        {
            Group group = SeedGroupWithMember(Group.CreateOneBill("Dinner", Group.OneBillDefaultEmoji, GroupTestData.ValidCurrency, GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment).Value);

            Result result = await ExecuteAsync(group, GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_IS_ONE_BILL);
            Assert.Equal(GroupStatus.Open, group.Status);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_GroupAlreadyClosing_FailsWith400GroupNotOpenAndKeepsTheFirstStart()
        {
            DateTimeOffset firstStart = GroupTestData.Moment.AddHours(-1);
            Group group = SeedGroupWithMember(GroupTestData.NewClosingGroup(firstStart));

            Result result = await ExecuteAsync(group, GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_OPEN);
            Assert.Equal(firstStart, group.ClosingStartedAt);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_ClosingThatRanOut24HoursAgo_FailsWith400GroupNotOpenBecauseItCountsAsFinished()
        {
            Group group = SeedGroupWithMember(GroupTestData.NewClosingGroup(GroupTestData.Moment.AddHours(-Group.ClosingFinishHours)));

            Result result = await ExecuteAsync(group, GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_OPEN);
        }

        [Fact]
        public async Task Execute_WhenSomeoneOwesMoney_FailsWith400GroupNotKvitAndStaysOpen()
        {
            Group group = SeedGroupWithMember(GroupTestData.NewGroup());
            Guid[] rowIds = [MoneyTestData.Filip, MoneyTestData.Ana];
            _money.AnswerWith(GroupMoneyTestData.Owing(rowIds, rowIds[0], rowIds[1]));

            Result result = await ExecuteAsync(group, GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_KVIT);
            Assert.Equal(GroupStatus.Open, group.Status);
            Assert.Null(group.ClosingStartedAt);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_WhenAPaymentIsWaitingForAnAnswer_FailsWith400GroupNotKvit()
        {
            Group group = SeedGroupWithMember(GroupTestData.NewGroup());
            Guid[] rowIds = [MoneyTestData.Filip, MoneyTestData.Ana];
            _money.AnswerWith(GroupMoneyTestData.PendingPayment(rowIds, rowIds[1], rowIds[0]));

            Result result = await ExecuteAsync(group, GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_KVIT);
            Assert.Equal(GroupStatus.Open, group.Status);
        }

        [Fact]
        public async Task Execute_AfterTheDebtWasPaidInFull_StartsTheClosing()
        {
            Group group = SeedGroupWithMember(GroupTestData.NewGroup());
            Guid[] rowIds = [MoneyTestData.Filip, MoneyTestData.Ana];
            _money.AnswerWith(GroupMoneyTestData.Settled(rowIds, rowIds[0], rowIds[1]));

            Result result = await ExecuteAsync(group, GroupTestData.OwnerId);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupStatus.Closing, group.Status);
            Assert.Equal([group.Id], _money.RequestedGroupIds);
        }

        private Group SeedGroupWithMember(Group group)
        {
            _groups.Seed(group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _members.Seed(MemberTestData.Account(group.Id, GroupTestData.OwnerId, "Filip"));
            _members.Seed(MemberTestData.Account(group.Id, GroupTestData.MemberId, "Ana"));

            return group;
        }

        private Task<Result> ExecuteAsync(Group group, Guid userId)
        {
            StartClosing startClosing = new(
                _groups,
                _members,
                _money,
                _activityEvents,
                new FinishExpiredClosing(_activityEvents),
                new FixedTimeProvider(GroupTestData.Moment));

            return startClosing.Execute(group.Id, userId, TestContext.Current.CancellationToken);
        }
    }
}
