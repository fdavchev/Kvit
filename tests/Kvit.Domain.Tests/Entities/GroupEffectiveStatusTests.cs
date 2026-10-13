using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupEffectiveStatusTests
    {
        private const int SecondsInADay = 24 * 60 * 60;

        [Fact]
        public void ClosingFinishHours_IsTwentyFour()
        {
            Assert.Equal(24, Group.ClosingFinishHours);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3_600)]
        [InlineData(SecondsInADay - 60)]
        [InlineData(SecondsInADay - 1)]
        public void EffectiveStatusAt_ClosingForLessThan24Hours_IsClosing(int secondsSinceStart)
        {
            Group group = GroupTestData.NewClosingGroup(GroupTestData.Moment);

            GroupStatus status = group.EffectiveStatusAt(GroupTestData.Moment.AddSeconds(secondsSinceStart));

            Assert.Equal(GroupStatus.Closing, status);
        }

        [Theory]
        [InlineData(SecondsInADay)]
        [InlineData(SecondsInADay + 1)]
        [InlineData(SecondsInADay * 2)]
        [InlineData(SecondsInADay * 30)]
        public void EffectiveStatusAt_Closing24HoursOrMore_IsFinished(int secondsSinceStart)
        {
            Group group = GroupTestData.NewClosingGroup(GroupTestData.Moment);

            GroupStatus status = group.EffectiveStatusAt(GroupTestData.Moment.AddSeconds(secondsSinceStart));

            Assert.Equal(GroupStatus.Finished, status);
        }

        [Fact]
        public void EffectiveStatusAt_At23Hours59Minutes_IsClosingAndAtExactly24Hours_IsFinished()
        {
            Group group = GroupTestData.NewClosingGroup(GroupTestData.Moment);

            GroupStatus justBefore = group.EffectiveStatusAt(GroupTestData.Moment.AddHours(23).AddMinutes(59));
            GroupStatus exactly = group.EffectiveStatusAt(GroupTestData.Moment.AddHours(24));

            Assert.Equal(GroupStatus.Closing, justBefore);
            Assert.Equal(GroupStatus.Finished, exactly);
        }

        [Fact]
        public void EffectiveStatusAt_ExpiredClosing_ChangesNothingInTheGroup()
        {
            Group group = GroupTestData.NewClosingGroup(GroupTestData.Moment);

            group.EffectiveStatusAt(GroupTestData.Moment.AddDays(3));

            Assert.Equal(GroupStatus.Closing, group.Status);
            Assert.Equal(GroupTestData.Moment, group.ClosingStartedAt);
            Assert.Null(group.FinishedAt);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(SecondsInADay)]
        [InlineData(SecondsInADay * 365)]
        public void EffectiveStatusAt_OpenGroup_IsOpenAtAnyTime(int secondsSinceCreation)
        {
            Group group = GroupTestData.NewGroup();

            GroupStatus status = group.EffectiveStatusAt(GroupTestData.Moment.AddSeconds(secondsSinceCreation));

            Assert.Equal(GroupStatus.Open, status);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(SecondsInADay * 365)]
        public void EffectiveStatusAt_FinishedGroup_IsFinishedAtAnyTime(int secondsSinceFinish)
        {
            Group group = GroupTestData.NewFinishedGroup(GroupTestData.Moment, GroupTestData.Moment.AddHours(1));

            GroupStatus status = group.EffectiveStatusAt(GroupTestData.Moment.AddHours(1).AddSeconds(secondsSinceFinish));

            Assert.Equal(GroupStatus.Finished, status);
        }

        [Fact]
        public void EffectiveStatusAt_AfterCancelClosing_IsOpenEvenLongAfterTheOldClosingTime()
        {
            Group group = GroupTestData.NewClosingGroup(GroupTestData.Moment);
            group.CancelClosing();

            GroupStatus status = group.EffectiveStatusAt(GroupTestData.Moment.AddDays(5));

            Assert.Equal(GroupStatus.Open, status);
        }

        [Fact]
        public void EffectiveStatusAt_AfterUnlock_IsOpenEvenLongAfterTheOldClosingTime()
        {
            Group group = GroupTestData.NewFinishedGroup(GroupTestData.Moment, GroupTestData.Moment.AddHours(1));
            group.Unlock();

            GroupStatus status = group.EffectiveStatusAt(GroupTestData.Moment.AddDays(5));

            Assert.Equal(GroupStatus.Open, status);
        }

        [Fact]
        public void EffectiveStatusAt_ClosingStartedAgainAfterUnlock_CountsFromTheNewStart()
        {
            Group group = GroupTestData.NewFinishedGroup(GroupTestData.Moment, GroupTestData.Moment.AddHours(1));
            group.Unlock();
            DateTimeOffset secondStart = GroupTestData.Moment.AddDays(10);
            group.StartClosing(secondStart);

            GroupStatus insideNewWindow = group.EffectiveStatusAt(secondStart.AddHours(23));
            GroupStatus afterNewWindow = group.EffectiveStatusAt(secondStart.AddHours(24));

            Assert.Equal(GroupStatus.Closing, insideNewWindow);
            Assert.Equal(GroupStatus.Finished, afterNewWindow);
        }
    }
}
