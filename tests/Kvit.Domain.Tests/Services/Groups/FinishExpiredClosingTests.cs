using Kvit.Domain.Entities;
using Kvit.Domain.Services.Groups;
using Kvit.Domain.Tests.Entities;
using Kvit.Domain.Tests.Fakes;
using Xunit;

namespace Kvit.Domain.Tests.Services.Groups
{
    public class FinishExpiredClosingTests
    {
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly FinishExpiredClosing _finishExpiredClosing;

        public FinishExpiredClosingTests()
        {
            _finishExpiredClosing = new FinishExpiredClosing(_activityEvents);
        }

        [Fact]
        public void Execute_ClosingThatRanOut_FinishesTheGroupAtTheEndOfThe24Hours()
        {
            Group group = GroupTestData.NewClosingGroup(GroupTestData.Moment);
            DateTimeOffset endsAt = GroupTestData.Moment.AddHours(Group.ClosingFinishHours);

            _finishExpiredClosing.Execute(group, endsAt.AddHours(5));

            Assert.Equal(GroupStatus.Finished, group.Status);
            Assert.Equal(endsAt, group.FinishedAt);
            Assert.Equal(GroupTestData.Moment, group.ClosingStartedAt);
        }

        [Fact]
        public void Execute_ClosingThatRanOut_WritesOneGroupFinishedEventByTheOwnerAtTheEndOfThe24Hours()
        {
            Group group = GroupTestData.NewClosingGroup(GroupTestData.Moment);
            DateTimeOffset endsAt = GroupTestData.Moment.AddHours(Group.ClosingFinishHours);

            _finishExpiredClosing.Execute(group, endsAt.AddHours(5));

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.GroupFinished, activityEvent.Type);
            Assert.Equal(group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(endsAt, activityEvent.CreatedAt);
        }

        [Fact]
        public void Execute_ExactlyAt24Hours_FinishesTheGroup()
        {
            Group group = GroupTestData.NewClosingGroup(GroupTestData.Moment);

            _finishExpiredClosing.Execute(group, GroupTestData.Moment.AddHours(Group.ClosingFinishHours));

            Assert.Equal(GroupStatus.Finished, group.Status);
        }

        [Fact]
        public void Execute_OneSecondBefore24Hours_ChangesNothing()
        {
            Group group = GroupTestData.NewClosingGroup(GroupTestData.Moment);

            _finishExpiredClosing.Execute(group, GroupTestData.Moment.AddHours(Group.ClosingFinishHours).AddSeconds(-1));

            Assert.Equal(GroupStatus.Closing, group.Status);
            Assert.Null(group.FinishedAt);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public void Execute_OpenGroup_ChangesNothing()
        {
            Group group = GroupTestData.NewGroup();

            _finishExpiredClosing.Execute(group, GroupTestData.Moment.AddDays(30));

            Assert.Equal(GroupStatus.Open, group.Status);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public void Execute_GroupThatIsAlreadyFinished_KeepsItsFinishTimeAndWritesNoSecondEvent()
        {
            DateTimeOffset finishedAt = GroupTestData.Moment.AddHours(2);
            Group group = GroupTestData.NewFinishedGroup(GroupTestData.Moment, finishedAt);

            _finishExpiredClosing.Execute(group, GroupTestData.Moment.AddDays(3));

            Assert.Equal(finishedAt, group.FinishedAt);
            Assert.Empty(_activityEvents.Added);
        }
    }
}
