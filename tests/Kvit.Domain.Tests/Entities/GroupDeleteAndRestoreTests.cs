using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupDeleteAndRestoreTests
    {
        private const int SecondsInThirtyDays = 30 * 24 * 60 * 60;

        [Fact]
        public void Delete_RecordsTheMomentAndWhoDeletedIt()
        {
            Group group = GroupTestData.NewGroup();

            group.Delete(GroupTestData.OwnerId, GroupTestData.Moment);

            Assert.Equal(GroupTestData.Moment, group.DeletedAt);
            Assert.Equal(GroupTestData.OwnerId, group.DeletedByUserId);
        }

        [Fact]
        public void Restore_GroupThatIsNotDeleted_FailsWithGroupNotDeleted()
        {
            Group group = GroupTestData.NewGroup();

            Result result = group.Restore(GroupTestData.Moment);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_DELETED);
        }

        [Fact]
        public void Restore_RightAfterTheDeletion_ClearsTheDeletion()
        {
            Group group = GroupTestData.NewDeletedGroup(GroupTestData.Moment);

            Result result = group.Restore(GroupTestData.Moment);

            Assert.True(result.IsSuccess);
            Assert.Null(group.DeletedAt);
            Assert.Null(group.DeletedByUserId);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(SecondsInThirtyDays - 1)]
        public void Restore_BeforeTheThirtyDaysAreOver_Succeeds(int secondsAfterDeletion)
        {
            Group group = GroupTestData.NewDeletedGroup(GroupTestData.Moment);

            Result result = group.Restore(GroupTestData.Moment.AddSeconds(secondsAfterDeletion));

            Assert.True(result.IsSuccess);
            Assert.Null(group.DeletedAt);
        }

        [Theory]
        [InlineData(SecondsInThirtyDays)]
        [InlineData(SecondsInThirtyDays + 1)]
        [InlineData(SecondsInThirtyDays * 2)]
        public void Restore_ExactlyThirtyDaysOrLater_FailsWithGroupRestoreExpiredAndKeepsTheDeletion(int secondsAfterDeletion)
        {
            Group group = GroupTestData.NewDeletedGroup(GroupTestData.Moment);

            Result result = group.Restore(GroupTestData.Moment.AddSeconds(secondsAfterDeletion));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_RESTORE_EXPIRED);
            Assert.Equal(GroupTestData.Moment, group.DeletedAt);
            Assert.Equal(GroupTestData.OwnerId, group.DeletedByUserId);
        }

        [Fact]
        public void RestorableUntil_IsThirtyDaysAfterTheDeletion()
        {
            DateTimeOffset restorableUntil = Group.RestorableUntil(GroupTestData.Moment);

            Assert.Equal(GroupTestData.Moment.AddDays(30), restorableUntil);
            Assert.Equal(30, Group.DeletedGroupRestoreDays);
        }
    }
}
