using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupRosterFindTests
    {
        private readonly GroupMember _account = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");
        private readonly GroupMember _plainName = MemberTestData.PlainName(MemberTestData.GroupId, "Grandma");
        private readonly GroupMember _claimed = MemberTestData.Claimed(MemberTestData.GroupId, MemberTestData.OtherUserId, "Marko");
        private readonly GroupMember _removedAccount = MemberTestData.Removed(MemberTestData.GroupId, GroupTestData.StrangerId, "Bojan");
        private readonly GroupMember _removedPlainName = MemberTestData.RemovedPlainName(MemberTestData.GroupId, "Aunt");
        private readonly GroupMember _leftAccount = MemberTestData.Left(MemberTestData.GroupId, Guid.NewGuid(), "Dana");
        private readonly GroupMember _setAsideAccount = MemberTestData.SetAside(MemberTestData.GroupId, Guid.NewGuid(), "Eli", GroupTestData.Moment.AddDays(-1));

        [Fact]
        public void FindCurrent_CurrentAccountPlainNameAndClaimedRows_AreFound()
        {
            GroupRoster roster = Roster();

            Assert.Same(_account, roster.FindCurrent(_account.Id).Value.Member);
            Assert.Same(_plainName, roster.FindCurrent(_plainName.Id).Value.Member);
            Assert.Same(_claimed, roster.FindCurrent(_claimed.Id).Value.Member);
        }

        [Theory]
        [InlineData("removedAccount")]
        [InlineData("removedPlainName")]
        [InlineData("left")]
        [InlineData("setAside")]
        public void FindCurrent_ARowThatEnded_FailsWith404MemberNotFound(string which)
        {
            GroupRoster roster = Roster();

            Result<NamedMember> result = roster.FindCurrent(IdOf(which));

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public void FindCurrent_AnIdThatIsNotInTheRoster_FailsWith404MemberNotFoundNamingTheId()
        {
            GroupRoster roster = Roster();

            Result<NamedMember> result = roster.FindCurrent(GroupTestData.StrangerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Contains(GroupTestData.StrangerId.ToString(), result.Error, StringComparison.Ordinal);
        }

        [Fact]
        public void FindCurrent_KeepsTheDisplayNameOfTheRow()
        {
            GroupRoster roster = new([MemberTestData.Named(_account, "Ana Petrova")]);

            Result<NamedMember> result = roster.FindCurrent(_account.Id);

            Assert.Equal("Ana Petrova", result.Value.DisplayName);
        }

        [Fact]
        public void FindCurrentPlainName_ACurrentPlainName_IsFound()
        {
            GroupRoster roster = Roster();

            Result<NamedMember> result = roster.FindCurrentPlainName(_plainName.Id);

            Assert.Same(_plainName, result.Value.Member);
        }

        [Theory]
        [InlineData("account")]
        [InlineData("claimed")]
        [InlineData("removedPlainName")]
        [InlineData("removedAccount")]
        public void FindCurrentPlainName_AnAccountAClaimedNameOrAnEndedRow_FailsWith404MemberNotFound(string which)
        {
            GroupRoster roster = Roster();

            Result<NamedMember> result = roster.FindCurrentPlainName(IdOf(which));

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public void FindCurrentPlainName_AClaimedNameWhoseClaimWasUndone_IsFoundAgain()
        {
            GroupMember undone = MemberTestData.Claimed(MemberTestData.GroupId, GroupTestData.MemberId, "Grandma");
            undone.UndoClaim();
            GroupRoster roster = MemberTestData.RosterOf(undone);

            Result<NamedMember> result = roster.FindCurrentPlainName(undone.Id);

            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("removedAccount")]
        [InlineData("removedPlainName")]
        public void FindRemoved_ARowEndedByRemoval_IsFound(string which)
        {
            GroupRoster roster = Roster();

            Result<NamedMember> result = roster.FindRemoved(IdOf(which));

            Assert.Equal(IdOf(which), result.Value.Member.Id);
        }

        [Theory]
        [InlineData("left")]
        [InlineData("setAside")]
        [InlineData("account")]
        [InlineData("plainName")]
        public void FindRemoved_ARowThatLeftWasSetAsideOrIsCurrent_FailsWith404MemberNotFound(string which)
        {
            GroupRoster roster = Roster();

            Result<NamedMember> result = roster.FindRemoved(IdOf(which));

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public void FindRemoved_AnIdThatIsNotInTheRoster_FailsWith404MemberNotFound()
        {
            GroupRoster roster = Roster();

            Result<NamedMember> result = roster.FindRemoved(GroupTestData.StrangerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        private GroupRoster Roster()
        {
            return MemberTestData.RosterOf(_account, _plainName, _claimed, _removedAccount, _removedPlainName, _leftAccount, _setAsideAccount);
        }

        private Guid IdOf(string which)
        {
            return which switch
            {
                "account" => _account.Id,
                "plainName" => _plainName.Id,
                "claimed" => _claimed.Id,
                "removedAccount" => _removedAccount.Id,
                "removedPlainName" => _removedPlainName.Id,
                "left" => _leftAccount.Id,
                "setAside" => _setAsideAccount.Id,
                _ => throw new ArgumentOutOfRangeException(nameof(which), which, "Unknown test row."),
            };
        }
    }
}
