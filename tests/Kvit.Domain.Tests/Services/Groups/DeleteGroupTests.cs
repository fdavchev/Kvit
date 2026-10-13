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
    public class DeleteGroupTests
    {
        private static readonly Guid[] TwoRowIds = [MoneyTestData.Filip, MoneyTestData.Ana];

        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMoneyRepository _money = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly DeleteGroup _deleteGroup;
        private readonly Group _group = GroupTestData.NewGroup();

        public DeleteGroupTests()
        {
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _deleteGroup = new DeleteGroup(_groups, _money, _activityEvents,new FixedTimeProvider(GroupTestData.Moment));
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

        [Fact]
        public async Task Execute_OwnerWhenEveryoneIsSettledUpAfterAConfirmedPayment_DeletesTheGroup()
        {
            _money.AnswerWith(GroupMoneyTestData.Settled(TwoRowIds, TwoRowIds[0], TwoRowIds[1]));

            Result result = await ExecuteAsync(GroupTestData.OwnerId);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.Moment, _group.DeletedAt);
            Assert.Single(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_OwnerWhenSomeoneStillOwesMoney_FailsWith400GroupBalancesNotZeroAndDeletesNothing()
        {
            _money.AnswerWith(GroupMoneyTestData.Owing(TwoRowIds, TwoRowIds[0], TwoRowIds[1]));

            Result result = await ExecuteAsync(GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            Assert.Null(_group.DeletedAt);
            Assert.Null(_group.DeletedByUserId);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_OwnerWhenAPaymentIsWaitingForAnAnswer_FailsWith400GroupHasPendingPaymentAndDeletesNothing()
        {
            _money.AnswerWith(GroupMoneyTestData.PendingPayment(TwoRowIds, TwoRowIds[0], TwoRowIds[1]));

            Result result = await ExecuteAsync(GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_HAS_PENDING_PAYMENT);
            Assert.Null(_group.DeletedAt);
            Assert.Null(_group.DeletedByUserId);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_MemberWhoIsNotTheOwnerWhileMoneyIsOpen_FailsWith403GroupNotOwnerBeforeTheMoneyIsChecked()
        {
            _money.AnswerWith(GroupMoneyTestData.Owing(TwoRowIds, TwoRowIds[0], TwoRowIds[1]));

            Result result = await ExecuteAsync(GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Empty(_money.RequestedGroupIds);
        }

        [Fact]
        public async Task Execute_Owner_AsksForTheMoneyOfThisGroupOnly()
        {
            await ExecuteAsync(GroupTestData.OwnerId);

            Assert.Equal([_group.Id], _money.RequestedGroupIds);
        }

        private Task<Result> ExecuteAsync(Guid userId)
        {
            return _deleteGroup.Execute(_group.Id, userId, TestContext.Current.CancellationToken);
        }
    }
}
