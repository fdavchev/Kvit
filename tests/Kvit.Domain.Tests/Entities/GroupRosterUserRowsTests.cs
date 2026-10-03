using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupRosterUserRowsTests
    {
        private static readonly DateTimeOffset Earlier = GroupTestData.Moment.AddDays(-5);
        private static readonly DateTimeOffset Later = GroupTestData.Moment.AddDays(-2);

        [Fact]
        public void CurrentRowOf_AUserWithACurrentRow_ReturnsThatRow()
        {
            GroupMember own = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");
            GroupRoster roster = MemberTestData.RosterOf(MemberTestData.Account(MemberTestData.GroupId, GroupTestData.OwnerId, "Filip"), own);

            NamedMember? row = roster.CurrentRowOf(GroupTestData.MemberId);

            Assert.NotNull(row);
            Assert.Same(own, row.Member);
        }

        [Fact]
        public void CurrentRowOf_AUserWhoseOldRowEndedAndWhoseClaimedRowIsCurrent_ReturnsTheCurrentOne()
        {
            GroupMember setAside = MemberTestData.SetAside(MemberTestData.GroupId, GroupTestData.MemberId, "Ana", Earlier);
            GroupMember claimed = MemberTestData.Claimed(MemberTestData.GroupId, GroupTestData.MemberId, "Grandma");
            GroupRoster roster = MemberTestData.RosterOf(setAside, claimed);

            NamedMember? row = roster.CurrentRowOf(GroupTestData.MemberId);

            Assert.NotNull(row);
            Assert.Same(claimed, row.Member);
        }

        [Fact]
        public void CurrentRowOf_AUserWhoOnlyHasEndedRows_ReturnsNull()
        {
            GroupRoster roster = MemberTestData.RosterOf(MemberTestData.Left(MemberTestData.GroupId, GroupTestData.MemberId, "Ana"));

            Assert.Null(roster.CurrentRowOf(GroupTestData.MemberId));
        }

        [Fact]
        public void CurrentRowOf_AUserWithNoRowAtAll_ReturnsNull()
        {
            GroupRoster roster = MemberTestData.RosterOf(MemberTestData.Account(MemberTestData.GroupId, GroupTestData.OwnerId, "Filip"));

            Assert.Null(roster.CurrentRowOf(GroupTestData.StrangerId));
        }

        [Fact]
        public void LatestEndedRowOf_SeveralRowsEndedTheSameWay_ReturnsTheOneThatEndedLast()
        {
            GroupMember older = MemberTestData.SetAside(MemberTestData.GroupId, GroupTestData.MemberId, "Ana", Earlier);
            GroupMember newer = MemberTestData.SetAside(MemberTestData.GroupId, GroupTestData.MemberId, "Ana", Later);
            GroupRoster roster = MemberTestData.RosterOf(newer, older);

            NamedMember? row = roster.LatestEndedRowOf(GroupTestData.MemberId, MemberEndKind.SetAside);

            Assert.NotNull(row);
            Assert.Same(newer, row.Member);
        }

        [Fact]
        public void LatestEndedRowOf_IgnoresRowsEndedInAnotherWay()
        {
            GroupMember left = MemberTestData.Left(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");
            GroupRoster roster = MemberTestData.RosterOf(left);

            Assert.Null(roster.LatestEndedRowOf(GroupTestData.MemberId, MemberEndKind.SetAside));
            Assert.Null(roster.LatestEndedRowOf(GroupTestData.MemberId, MemberEndKind.Removed));
            Assert.Same(left, roster.LatestEndedRowOf(GroupTestData.MemberId, MemberEndKind.Left)?.Member);
        }

        [Fact]
        public void LatestEndedRowOf_IgnoresRowsOfOtherUsersAndCurrentRows()
        {
            GroupMember otherUsersRow = MemberTestData.SetAside(MemberTestData.GroupId, MemberTestData.OtherUserId, "Marko", Later);
            GroupMember currentRow = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");
            GroupRoster roster = MemberTestData.RosterOf(otherUsersRow, currentRow);

            Assert.Null(roster.LatestEndedRowOf(GroupTestData.MemberId, MemberEndKind.SetAside));
        }

        [Fact]
        public void HasAnyRowOf_ACurrentRowAnEndedRowAndAClaimedRow_AreAllCounted()
        {
            GroupRoster current = MemberTestData.RosterOf(MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana"));
            GroupRoster ended = MemberTestData.RosterOf(MemberTestData.Left(MemberTestData.GroupId, GroupTestData.MemberId, "Ana"));
            GroupRoster claimed = MemberTestData.RosterOf(MemberTestData.Claimed(MemberTestData.GroupId, GroupTestData.MemberId, "Grandma"));

            Assert.True(current.HasAnyRowOf(GroupTestData.MemberId));
            Assert.True(ended.HasAnyRowOf(GroupTestData.MemberId));
            Assert.True(claimed.HasAnyRowOf(GroupTestData.MemberId));
        }

        [Fact]
        public void HasAnyRowOf_AUserWithNoRowAndPlainNames_IsFalse()
        {
            GroupRoster roster = MemberTestData.RosterOf(
                MemberTestData.Account(MemberTestData.GroupId, GroupTestData.OwnerId, "Filip"),
                MemberTestData.PlainName(MemberTestData.GroupId, "Grandma"));

            Assert.False(roster.HasAnyRowOf(GroupTestData.MemberId));
        }
    }
}
