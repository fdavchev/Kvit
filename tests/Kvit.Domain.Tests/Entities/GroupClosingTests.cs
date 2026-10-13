using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupClosingTests
    {
        [Fact]
        public void NewGroup_IsOpenWithNoClosingTimeAndNoFinishTime()
        {
            Group group = GroupTestData.NewGroup();

            Assert.Equal(GroupStatus.Open, group.Status);
            Assert.Null(group.ClosingStartedAt);
            Assert.Null(group.FinishedAt);
        }

        [Fact]
        public void StartClosing_OpenGroup_BecomesClosingAndRecordsTheMoment()
        {
            Group group = GroupTestData.NewGroup();

            group.StartClosing(GroupTestData.Moment);

            Assert.Equal(GroupStatus.Closing, group.Status);
            Assert.Equal(GroupTestData.Moment, group.ClosingStartedAt);
            Assert.Null(group.FinishedAt);
        }

        [Fact]
        public void Finish_ClosingGroup_BecomesFinishedRecordsTheMomentAndKeepsTheClosingTime()
        {
            Group group = GroupTestData.NewClosingGroup(GroupTestData.Moment);
            DateTimeOffset finishedAt = GroupTestData.Moment.AddHours(5);

            group.Finish(finishedAt);

            Assert.Equal(GroupStatus.Finished, group.Status);
            Assert.Equal(finishedAt, group.FinishedAt);
            Assert.Equal(GroupTestData.Moment, group.ClosingStartedAt);
        }

        [Fact]
        public void Finish_AtTheSameMomentAsStartClosing_IsHowAGroupWithNobodyToAskFinishesAtOnce()
        {
            Group group = GroupTestData.NewGroup();

            group.StartClosing(GroupTestData.Moment);
            group.Finish(GroupTestData.Moment);

            Assert.Equal(GroupStatus.Finished, group.Status);
            Assert.Equal(GroupTestData.Moment, group.ClosingStartedAt);
            Assert.Equal(GroupTestData.Moment, group.FinishedAt);
        }

        [Fact]
        public void CancelClosing_ClosingGroup_BecomesOpenAndClearsTheClosingTime()
        {
            Group group = GroupTestData.NewClosingGroup(GroupTestData.Moment);

            group.CancelClosing();

            Assert.Equal(GroupStatus.Open, group.Status);
            Assert.Null(group.ClosingStartedAt);
            Assert.Null(group.FinishedAt);
        }

        [Fact]
        public void Unlock_FinishedGroup_BecomesOpenAndClearsBothTimes()
        {
            Group group = GroupTestData.NewFinishedGroup(GroupTestData.Moment, GroupTestData.Moment.AddHours(3));

            group.Unlock();

            Assert.Equal(GroupStatus.Open, group.Status);
            Assert.Null(group.ClosingStartedAt);
            Assert.Null(group.FinishedAt);
        }

        [Fact]
        public void StartClosing_AfterAnUnlock_RecordsTheNewMomentAndNotTheOldOne()
        {
            Group group = GroupTestData.NewFinishedGroup(GroupTestData.Moment, GroupTestData.Moment.AddHours(3));
            group.Unlock();
            DateTimeOffset secondStart = GroupTestData.Moment.AddDays(2);

            group.StartClosing(secondStart);

            Assert.Equal(GroupStatus.Closing, group.Status);
            Assert.Equal(secondStart, group.ClosingStartedAt);
            Assert.Null(group.FinishedAt);
        }

        [Fact]
        public void ClosingTransitions_DoNotTouchTheOwnerTheNameOrTheDeletion()
        {
            Group group = GroupTestData.NewGroup();

            group.StartClosing(GroupTestData.Moment);
            group.Finish(GroupTestData.Moment.AddHours(1));
            group.Unlock();

            Assert.Equal(GroupTestData.OwnerId, group.OwnerUserId);
            Assert.Equal(GroupTestData.ValidName, group.Name);
            Assert.Null(group.DeletedAt);
        }
    }
}
