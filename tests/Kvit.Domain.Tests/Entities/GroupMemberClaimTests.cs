using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupMemberClaimTests
    {
        [Fact]
        public void CheckCanClaimNames_AnOrdinaryAccountRow_Succeeds()
        {
            GroupMember member = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");

            Result result = member.CheckCanClaimNames();

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void CheckCanClaimNames_ARowThatHoldsAClaimedName_FailsWith400MemberCannotClaim()
        {
            GroupMember claimed = MemberTestData.Claimed(MemberTestData.GroupId, GroupTestData.MemberId, "Grandma");

            Result result = claimed.CheckCanClaimNames();

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
        }

        [Fact]
        public void Claim_PutsTheAccountOnThePlainNameKeepingTheName()
        {
            GroupMember plainName = MemberTestData.PlainName(MemberTestData.GroupId, "Grandma");

            plainName.Claim(GroupTestData.MemberId, GroupTestData.Moment);

            Assert.Equal(GroupTestData.MemberId, plainName.UserId);
            Assert.Equal(GroupTestData.Moment, plainName.ClaimedAt);
            Assert.Equal("Grandma", plainName.Name);
            Assert.Null(plainName.RemovedAt);
        }

        [Fact]
        public void UndoClaim_AClaimedRow_BecomesAPlainNameAgainAndReturnsTheClaimersAccount()
        {
            GroupMember claimed = MemberTestData.Claimed(MemberTestData.GroupId, GroupTestData.MemberId, "Grandma");

            Result<Guid> result = claimed.UndoClaim();

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.MemberId, result.Value);
            Assert.Null(claimed.UserId);
            Assert.Null(claimed.ClaimedAt);
            Assert.Equal("Grandma", claimed.Name);
        }

        [Fact]
        public void UndoClaim_AfterTheClaimIsUndone_TheRowCanBeClaimedAgain()
        {
            GroupMember claimed = MemberTestData.Claimed(MemberTestData.GroupId, GroupTestData.MemberId, "Grandma");
            claimed.UndoClaim();

            claimed.Claim(MemberTestData.OtherUserId, GroupTestData.Moment);

            Assert.Equal(MemberTestData.OtherUserId, claimed.UserId);
            Assert.Equal(GroupTestData.Moment, claimed.ClaimedAt);
        }

        [Fact]
        public void UndoClaim_APlainNameThatWasNeverClaimed_FailsWith400MemberNotClaimed()
        {
            GroupMember plainName = MemberTestData.PlainName(MemberTestData.GroupId, "Grandma");

            Result<Guid> result = plainName.UndoClaim();

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_CLAIMED);
        }

        [Fact]
        public void UndoClaim_AnOrdinaryAccountRow_FailsWith400MemberNotClaimedAndKeepsTheAccount()
        {
            GroupMember member = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");

            Result<Guid> result = member.UndoClaim();

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_CLAIMED);
            Assert.Equal(GroupTestData.MemberId, member.UserId);
        }
    }
}
