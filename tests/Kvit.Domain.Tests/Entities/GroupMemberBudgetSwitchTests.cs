using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupMemberBudgetSwitchTests
    {
        [Fact]
        public void ForAccount_CountsInBudgetByDefault()
        {
            GroupMember member = GroupMember.ForAccount(MemberTestData.GroupId, GroupTestData.MemberId, "Ana", GroupTestData.OwnerId, GroupTestData.Moment);

            Assert.True(member.CountsInBudget);
        }

        [Fact]
        public void ForName_CountsInBudgetByDefault()
        {
            GroupMember member = GroupMember.ForName(MemberTestData.GroupId, "Marko", GroupTestData.OwnerId, GroupTestData.Moment).Value;

            Assert.True(member.CountsInBudget);
        }

        [Fact]
        public void SetCountsInBudget_False_TurnsTheSwitchOff()
        {
            GroupMember member = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");

            member.SetCountsInBudget(false);

            Assert.False(member.CountsInBudget);
        }

        [Fact]
        public void SetCountsInBudget_TrueAfterFalse_TurnsTheSwitchBackOn()
        {
            GroupMember member = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");
            member.SetCountsInBudget(false);

            member.SetCountsInBudget(true);

            Assert.True(member.CountsInBudget);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SetCountsInBudget_TheSameValueTwice_KeepsTheValue(bool countsInBudget)
        {
            GroupMember member = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");

            member.SetCountsInBudget(countsInBudget);
            member.SetCountsInBudget(countsInBudget);

            Assert.Equal(countsInBudget, member.CountsInBudget);
        }

        [Fact]
        public void SetCountsInBudget_OnAClaimedName_ChangesOnlyTheFlag()
        {
            GroupMember member = MemberTestData.Claimed(MemberTestData.GroupId, GroupTestData.MemberId, "Marko");
            object?[] before = OtherFieldsOf(member);

            member.SetCountsInBudget(false);

            Assert.Equal(before, OtherFieldsOf(member));
            Assert.NotNull(member.ClaimedAt);
        }

        [Fact]
        public void SetCountsInBudget_OnARemovedMember_ChangesOnlyTheFlag()
        {
            GroupMember member = MemberTestData.Removed(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");
            object?[] before = OtherFieldsOf(member);

            member.SetCountsInBudget(false);

            Assert.Equal(before, OtherFieldsOf(member));
            Assert.NotNull(member.RemovedAt);
        }

        private static object?[] OtherFieldsOf(GroupMember member)
        {
            return [member.Id, member.GroupId, member.UserId, member.Name, member.JoinedAt, member.AddedByUserId, member.ClaimedAt, member.RemovedAt, member.RemovedByUserId, member.EndKind];
        }
    }
}
