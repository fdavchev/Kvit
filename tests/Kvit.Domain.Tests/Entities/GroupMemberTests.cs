using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupMemberTests
    {
        private static readonly Guid GroupId = Guid.Parse("0199a1b2-c3d4-7e5f-8a6b-7c8d9e0f1a30");

        [Fact]
        public void ForAccount_SetsTheGroupTheAccountTheNameWhoAddedItAndTheJoinMoment()
        {
            GroupMember member = GroupMember.ForAccount(GroupId, GroupTestData.MemberId, "Ana", GroupTestData.OwnerId, GroupTestData.Moment);

            Assert.NotEqual(Guid.Empty, member.Id);
            Assert.Equal(GroupId, member.GroupId);
            Assert.Equal(GroupTestData.MemberId, member.UserId);
            Assert.Equal("Ana", member.Name);
            Assert.Equal(GroupTestData.OwnerId, member.AddedByUserId);
            Assert.Equal(GroupTestData.Moment, member.JoinedAt);
        }

        [Fact]
        public void ForAccount_StartsAsACurrentMemberThatWasNeverClaimed()
        {
            GroupMember member = GroupMember.ForAccount(GroupId, GroupTestData.MemberId, "Ana", GroupTestData.OwnerId, GroupTestData.Moment);

            Assert.Null(member.RemovedAt);
            Assert.Null(member.RemovedByUserId);
            Assert.Null(member.EndKind);
            Assert.Null(member.ClaimedAt);
        }

        [Fact]
        public void ForAccount_TwoMembers_GetDifferentIds()
        {
            GroupMember first = GroupMember.ForAccount(GroupId, GroupTestData.OwnerId, "Ana", GroupTestData.OwnerId, GroupTestData.Moment);
            GroupMember second = GroupMember.ForAccount(GroupId, GroupTestData.MemberId, "Marko", GroupTestData.OwnerId, GroupTestData.Moment);

            Assert.NotEqual(first.Id, second.Id);
        }
    }
}
