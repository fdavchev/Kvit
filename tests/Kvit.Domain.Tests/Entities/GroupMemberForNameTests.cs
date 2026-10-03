using System.Net;
using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupMemberForNameTests
    {
        [Fact]
        public void ForName_ValidName_MakesACurrentRowWithNoAccountThatWasNeverClaimed()
        {
            Result<GroupMember> result = GroupMember.ForName(MemberTestData.GroupId, "Grandma", GroupTestData.MemberId, GroupTestData.Moment);

            Assert.True(result.IsSuccess);
            GroupMember member = result.Value;
            Assert.NotEqual(Guid.Empty, member.Id);
            Assert.Equal(MemberTestData.GroupId, member.GroupId);
            Assert.Null(member.UserId);
            Assert.Equal("Grandma", member.Name);
            Assert.Equal(GroupTestData.MemberId, member.AddedByUserId);
            Assert.Equal(GroupTestData.Moment, member.JoinedAt);
            Assert.Null(member.ClaimedAt);
            Assert.Null(member.RemovedAt);
            Assert.Null(member.RemovedByUserId);
            Assert.Null(member.EndKind);
        }

        [Fact]
        public void ForName_NameWithSpacesAround_StoresTheTrimmedName()
        {
            Result<GroupMember> result = GroupMember.ForName(MemberTestData.GroupId, "  Grandma  ", GroupTestData.MemberId, GroupTestData.Moment);

            Assert.Equal("Grandma", result.Value.Name);
        }

        [Theory]
        [InlineData("G")]
        [InlineData("Баба")]
        public void ForName_ShortNames_AreAccepted(string name)
        {
            Result<GroupMember> result = GroupMember.ForName(MemberTestData.GroupId, name, GroupTestData.MemberId, GroupTestData.Moment);

            Assert.True(result.IsSuccess);
            Assert.Equal(name, result.Value.Name);
        }

        [Fact]
        public void ForName_NameOfTheMaximumLength_IsAccepted()
        {
            string name = new('a', AccountRules.DisplayNameMaxLength);

            Result<GroupMember> result = GroupMember.ForName(MemberTestData.GroupId, name, GroupTestData.MemberId, GroupTestData.Moment);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ForName_NameOneCharacterTooLong_FailsWith400MemberNameInvalid()
        {
            string name = new('a', AccountRules.DisplayNameMaxLength + 1);

            Result<GroupMember> result = GroupMember.ForName(MemberTestData.GroupId, name, GroupTestData.MemberId, GroupTestData.Moment);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_INVALID);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void ForName_EmptyOrBlankName_FailsWith400MemberNameInvalid(string name)
        {
            Result<GroupMember> result = GroupMember.ForName(MemberTestData.GroupId, name, GroupTestData.MemberId, GroupTestData.Moment);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_INVALID);
        }

        [Fact]
        public void ForName_NameOfTheMaximumLengthPaddedWithSpaces_IsAcceptedBecauseTheLengthIsCountedAfterTrimming()
        {
            string name = "  " + new string('a', AccountRules.DisplayNameMaxLength) + "  ";

            Result<GroupMember> result = GroupMember.ForName(MemberTestData.GroupId, name, GroupTestData.MemberId, GroupTestData.Moment);

            Assert.True(result.IsSuccess);
            Assert.Equal(AccountRules.DisplayNameMaxLength, result.Value.Name.Length);
        }

        [Fact]
        public void NotFound_Answers404MemberNotFoundNamingTheMemberId()
        {
            Result<GroupMember> result = GroupMember.NotFound<GroupMember>(GroupTestData.StrangerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Contains(GroupTestData.StrangerId.ToString(), result.Error, StringComparison.Ordinal);
        }
    }
}
