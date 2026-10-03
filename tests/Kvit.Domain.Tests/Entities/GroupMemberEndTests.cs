using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupMemberEndTests
    {
        private static readonly DateTimeOffset EndedAt = GroupTestData.Moment.AddHours(1);

        [Fact]
        public void Remove_AnAccountThatIsNotTheOwner_EndsTheRowAsRemovedByTheCaller()
        {
            GroupMember member = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");

            Result result = member.Remove(GroupTestData.OwnerId, GroupTestData.OwnerId, EndedAt);

            Assert.True(result.IsSuccess);
            Assert.Equal(EndedAt, member.RemovedAt);
            Assert.Equal(GroupTestData.OwnerId, member.RemovedByUserId);
            Assert.Equal(MemberEndKind.Removed, member.EndKind);
        }

        [Fact]
        public void Remove_APlainName_EndsTheRowAsRemoved()
        {
            GroupMember member = MemberTestData.PlainName(MemberTestData.GroupId, "Grandma");

            Result result = member.Remove(GroupTestData.OwnerId, GroupTestData.OwnerId, EndedAt);

            Assert.True(result.IsSuccess);
            Assert.Equal(MemberEndKind.Removed, member.EndKind);
        }

        [Fact]
        public void Remove_TheOwner_FailsWith400MemberIsOwnerAndChangesNothing()
        {
            GroupMember owner = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.OwnerId, "Filip");

            Result result = owner.Remove(GroupTestData.OwnerId, GroupTestData.OwnerId, EndedAt);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_IS_OWNER);
            Assert.Null(owner.RemovedAt);
            Assert.Null(owner.RemovedByUserId);
            Assert.Null(owner.EndKind);
        }

        [Fact]
        public void Leave_AnAccountThatIsNotTheOwner_EndsTheRowAsLeftByTheMemberThemselves()
        {
            GroupMember member = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");

            Result result = member.Leave(GroupTestData.OwnerId, EndedAt);

            Assert.True(result.IsSuccess);
            Assert.Equal(EndedAt, member.RemovedAt);
            Assert.Equal(GroupTestData.MemberId, member.RemovedByUserId);
            Assert.Equal(MemberEndKind.Left, member.EndKind);
        }

        [Fact]
        public void Leave_TheOwner_FailsWith400MemberOwnerCannotLeaveAndChangesNothing()
        {
            GroupMember owner = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.OwnerId, "Filip");

            Result result = owner.Leave(GroupTestData.OwnerId, EndedAt);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_OWNER_CANNOT_LEAVE);
            Assert.Null(owner.RemovedAt);
            Assert.Null(owner.EndKind);
        }

        [Fact]
        public void Leave_APlainName_ThrowsBecauseOnlyAnAccountCanLeave()
        {
            GroupMember plainName = MemberTestData.PlainName(MemberTestData.GroupId, "Grandma");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => plainName.Leave(GroupTestData.OwnerId, EndedAt));

            Assert.Contains(plainName.Id.ToString(), exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void SetAside_AnAccount_EndsTheRowAsSetAsideByTheMemberThemselves()
        {
            GroupMember member = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");

            member.SetAside(EndedAt);

            Assert.Equal(EndedAt, member.RemovedAt);
            Assert.Equal(GroupTestData.MemberId, member.RemovedByUserId);
            Assert.Equal(MemberEndKind.SetAside, member.EndKind);
        }

        [Fact]
        public void SetAside_APlainName_ThrowsBecauseOnlyAnAccountCanBeSetAside()
        {
            GroupMember plainName = MemberTestData.PlainName(MemberTestData.GroupId, "Grandma");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => plainName.SetAside(EndedAt));

            Assert.Contains(plainName.Id.ToString(), exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(MemberEndKind.Removed)]
        [InlineData(MemberEndKind.Left)]
        [InlineData(MemberEndKind.SetAside)]
        public void Reinstate_AnEndedRow_MakesItCurrentAgainWhateverEndedIt(MemberEndKind endKind)
        {
            GroupMember member = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");
            EndRow(member, endKind);

            member.Reinstate();

            Assert.Null(member.RemovedAt);
            Assert.Null(member.RemovedByUserId);
            Assert.Null(member.EndKind);
        }

        [Fact]
        public void Reinstate_ARowThatIsAlreadyCurrent_ChangesNothing()
        {
            GroupMember member = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");

            member.Reinstate();

            Assert.Null(member.RemovedAt);
            Assert.Null(member.EndKind);
            Assert.Equal(GroupTestData.MemberId, member.UserId);
        }

        [Fact]
        public void Reinstate_KeepsTheNameTheJoinMomentAndTheClaim()
        {
            GroupMember claimed = MemberTestData.Claimed(MemberTestData.GroupId, GroupTestData.MemberId, "Grandma");
            DateTimeOffset joinedAt = claimed.JoinedAt;
            DateTimeOffset? claimedAt = claimed.ClaimedAt;
            claimed.Leave(GroupTestData.OwnerId, EndedAt);

            claimed.Reinstate();

            Assert.Equal("Grandma", claimed.Name);
            Assert.Equal(joinedAt, claimed.JoinedAt);
            Assert.Equal(claimedAt, claimed.ClaimedAt);
            Assert.Equal(GroupTestData.MemberId, claimed.UserId);
        }

        private static void EndRow(GroupMember member, MemberEndKind endKind)
        {
            switch (endKind)
            {
                case MemberEndKind.Removed:
                    member.Remove(GroupTestData.OwnerId, GroupTestData.OwnerId, EndedAt);
                    break;
                case MemberEndKind.Left:
                    member.Leave(GroupTestData.OwnerId, EndedAt);
                    break;
                case MemberEndKind.SetAside:
                    member.SetAside(EndedAt);
                    break;
            }
        }
    }
}
