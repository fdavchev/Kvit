using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupOwnershipTests
    {
        [Fact]
        public void TransferOwnership_ToAnAccount_MakesThatAccountTheOwner()
        {
            Group group = GroupTestData.NewGroup();
            GroupMember newOwner = MemberTestData.Account(group.Id, GroupTestData.MemberId, "Ana");

            Result result = group.TransferOwnership(newOwner);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.MemberId, group.OwnerUserId);
        }

        [Fact]
        public void TransferOwnership_ToAClaimedName_MakesTheClaimersAccountTheOwner()
        {
            Group group = GroupTestData.NewGroup();
            GroupMember claimed = MemberTestData.Claimed(group.Id, GroupTestData.MemberId, "Grandma");

            Result result = group.TransferOwnership(claimed);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.MemberId, group.OwnerUserId);
        }

        [Fact]
        public void TransferOwnership_ToAPlainName_FailsWith400MemberNotAccountAndKeepsTheOwner()
        {
            Group group = GroupTestData.NewGroup();
            GroupMember plainName = MemberTestData.PlainName(group.Id, "Grandma");

            Result result = group.TransferOwnership(plainName);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_ACCOUNT);
            Assert.Equal(GroupTestData.OwnerId, group.OwnerUserId);
        }

        [Fact]
        public void TransferOwnership_ToTheCurrentOwner_FailsWith400MemberAlreadyOwner()
        {
            Group group = GroupTestData.NewGroup();
            GroupMember ownerRow = MemberTestData.Account(group.Id, GroupTestData.OwnerId, "Filip");

            Result result = group.TransferOwnership(ownerRow);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_ALREADY_OWNER);
            Assert.Equal(GroupTestData.OwnerId, group.OwnerUserId);
        }

        [Fact]
        public void TransferOwnership_BackToTheFormerOwner_Works()
        {
            Group group = GroupTestData.NewGroup();
            group.TransferOwnership(MemberTestData.Account(group.Id, GroupTestData.MemberId, "Ana"));

            Result result = group.TransferOwnership(MemberTestData.Account(group.Id, GroupTestData.OwnerId, "Filip"));

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.OwnerId, group.OwnerUserId);
        }

        [Fact]
        public void TransferOwnership_AfterTheTransfer_OnlyTheNewOwnerPassesTheOwnerCheck()
        {
            Group group = GroupTestData.NewGroup();
            group.TransferOwnership(MemberTestData.Account(group.Id, GroupTestData.MemberId, "Ana"));

            Assert.True(group.CheckOwner(GroupTestData.MemberId).IsSuccess);
            ResultAssert.Failed(group.CheckOwner(GroupTestData.OwnerId), HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }
    }
}
