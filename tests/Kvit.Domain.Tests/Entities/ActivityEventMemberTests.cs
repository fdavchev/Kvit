using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class ActivityEventMemberTests
    {
        private static readonly Guid MemberRowId = Guid.Parse("0199a1b2-c3d4-7e5f-8a6b-7c8d9e0f1a40");

        [Fact]
        public void MemberAdded_LinksTheMemberAndPutsOnlyTheNameInTheData()
        {
            ActivityEvent activityEvent = ActivityEvent.MemberAdded(MemberTestData.GroupId, GroupTestData.MemberId, MemberRowId, "Grandma", GroupTestData.Moment);

            AssertMemberEvent(activityEvent, ActivityEventType.MemberAdded, "Grandma");
        }

        [Fact]
        public void MemberJoined_LinksTheMemberAndPutsOnlyTheNameInTheData()
        {
            ActivityEvent activityEvent = ActivityEvent.MemberJoined(MemberTestData.GroupId, GroupTestData.MemberId, MemberRowId, "Ana", GroupTestData.Moment);

            AssertMemberEvent(activityEvent, ActivityEventType.MemberJoined, "Ana");
        }

        [Fact]
        public void MemberRemoved_LinksTheMemberAndPutsOnlyTheNameInTheData()
        {
            ActivityEvent activityEvent = ActivityEvent.MemberRemoved(MemberTestData.GroupId, GroupTestData.MemberId, MemberRowId, "Ana", GroupTestData.Moment);

            AssertMemberEvent(activityEvent, ActivityEventType.MemberRemoved, "Ana");
        }

        [Fact]
        public void MemberLeft_LinksTheMemberAndPutsOnlyTheNameInTheData()
        {
            ActivityEvent activityEvent = ActivityEvent.MemberLeft(MemberTestData.GroupId, GroupTestData.MemberId, MemberRowId, "Ana", GroupTestData.Moment);

            AssertMemberEvent(activityEvent, ActivityEventType.MemberLeft, "Ana");
        }

        [Fact]
        public void MemberLetBackIn_LinksTheMemberAndPutsOnlyTheNameInTheData()
        {
            ActivityEvent activityEvent = ActivityEvent.MemberLetBackIn(MemberTestData.GroupId, GroupTestData.OwnerId, MemberRowId, "Ana", GroupTestData.Moment);

            AssertMemberEvent(activityEvent, ActivityEventType.MemberLetBackIn, "Ana", GroupTestData.OwnerId);
        }

        [Fact]
        public void OwnershipTransferred_LinksTheNewOwnerAndPutsTheirNameInTheData()
        {
            ActivityEvent activityEvent = ActivityEvent.OwnershipTransferred(MemberTestData.GroupId, GroupTestData.OwnerId, MemberRowId, "Ana", GroupTestData.Moment);

            AssertMemberEvent(activityEvent, ActivityEventType.OwnershipTransferred, "Ana", GroupTestData.OwnerId);
        }

        [Theory]
        [InlineData("Grandma")]
        [InlineData("Баба \"Ката\"")]
        public void MemberAdded_KeepsTheNameExactlyIncludingQuotesAndCyrillic(string memberName)
        {
            ActivityEvent activityEvent = ActivityEvent.MemberAdded(MemberTestData.GroupId, GroupTestData.MemberId, MemberRowId, memberName, GroupTestData.Moment);

            Assert.Equal(memberName, ActivityEventData.Name(activityEvent));
        }

        [Fact]
        public void MemberClaimed_PutsTheClaimersNameAndTheClaimedNameInTheData()
        {
            ActivityEvent activityEvent = ActivityEvent.MemberClaimed(MemberTestData.GroupId, GroupTestData.MemberId, MemberRowId, "Ana", "Grandma", GroupTestData.Moment);

            AssertClaimEvent(activityEvent, ActivityEventType.MemberClaimed, GroupTestData.MemberId);
        }

        [Fact]
        public void ClaimUndone_PutsTheClaimersNameAndTheClaimedNameInTheData()
        {
            ActivityEvent activityEvent = ActivityEvent.ClaimUndone(MemberTestData.GroupId, GroupTestData.OwnerId, MemberRowId, "Ana", "Grandma", GroupTestData.Moment);

            AssertClaimEvent(activityEvent, ActivityEventType.ClaimUndone, GroupTestData.OwnerId);
        }

        [Fact]
        public void InviteLinkReset_HasTheTypeAndNoDataChangesOrMember()
        {
            ActivityEvent activityEvent = ActivityEvent.InviteLinkReset(MemberTestData.GroupId, GroupTestData.OwnerId, GroupTestData.Moment);

            AssertInviteEvent(activityEvent, ActivityEventType.InviteLinkReset);
        }

        [Fact]
        public void InviteLinkRestored_HasTheTypeAndNoDataChangesOrMember()
        {
            ActivityEvent activityEvent = ActivityEvent.InviteLinkRestored(MemberTestData.GroupId, GroupTestData.OwnerId, GroupTestData.Moment);

            AssertInviteEvent(activityEvent, ActivityEventType.InviteLinkRestored);
        }

        private static void AssertMemberEvent(ActivityEvent activityEvent, ActivityEventType expectedType, string expectedName, Guid? expectedActor = null)
        {
            Assert.NotEqual(Guid.Empty, activityEvent.Id);
            Assert.Equal(expectedType, activityEvent.Type);
            Assert.Equal(MemberTestData.GroupId, activityEvent.GroupId);
            Assert.Equal(expectedActor ?? GroupTestData.MemberId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal(MemberRowId, activityEvent.MemberId);
            Assert.Null(activityEvent.Changes);
            Assert.Null(activityEvent.ExpenseId);
            Assert.Null(activityEvent.SettlementId);
            Assert.Equal(["name"], ActivityEventData.PropertyNames(activityEvent));
            Assert.Equal(expectedName, ActivityEventData.Name(activityEvent));
        }

        private static void AssertClaimEvent(ActivityEvent activityEvent, ActivityEventType expectedType, Guid expectedActor)
        {
            Assert.Equal(expectedType, activityEvent.Type);
            Assert.Equal(MemberTestData.GroupId, activityEvent.GroupId);
            Assert.Equal(expectedActor, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal(MemberRowId, activityEvent.MemberId);
            Assert.Null(activityEvent.Changes);
            Assert.Equal(["name", "claimedName"], ActivityEventData.PropertyNames(activityEvent));
            Assert.Equal("Ana", ActivityEventData.Name(activityEvent));
            Assert.Equal("Grandma", ActivityEventData.ClaimedName(activityEvent));
        }

        private static void AssertInviteEvent(ActivityEvent activityEvent, ActivityEventType expectedType)
        {
            Assert.NotEqual(Guid.Empty, activityEvent.Id);
            Assert.Equal(expectedType, activityEvent.Type);
            Assert.Equal(MemberTestData.GroupId, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Null(activityEvent.Data);
            Assert.Null(activityEvent.Changes);
            Assert.Null(activityEvent.MemberId);
        }
    }
}
