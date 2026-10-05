using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupRosterNameTests
    {
        [Fact]
        public void CheckNameFree_AnEmptyRoster_Succeeds()
        {
            GroupRoster roster = MemberTestData.RosterOf();

            Result result = roster.CheckNameFree("Marko");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void CheckNameFree_ANameNoOneShows_Succeeds()
        {
            GroupRoster roster = MemberTestData.RosterOf(MemberTestData.PlainName(MemberTestData.GroupId, "Grandma"));

            Result result = roster.CheckNameFree("Marko");

            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("Marko")]
        [InlineData("marko")]
        [InlineData("MARKO")]
        [InlineData("mArKo")]
        public void CheckNameFree_ANameACurrentPlainNameAlreadyShows_FailsWith400MemberNameTakenWhateverTheLetterCase(string name)
        {
            GroupRoster roster = MemberTestData.RosterOf(MemberTestData.PlainName(MemberTestData.GroupId, "Marko"));

            Result result = roster.CheckNameFree(name);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
        }

        [Theory]
        [InlineData("Марко")]
        [InlineData("марко")]
        [InlineData("МАРКО")]
        public void CheckNameFree_ACyrillicNameInAnotherLetterCase_FailsWith400MemberNameTaken(string name)
        {
            GroupRoster roster = MemberTestData.RosterOf(MemberTestData.PlainName(MemberTestData.GroupId, "Марко"));

            Result result = roster.CheckNameFree(name);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
        }

        [Fact]
        public void CheckNameFree_TheNameOfACurrentAccount_FailsWith400MemberNameTaken()
        {
            GroupRoster roster = MemberTestData.RosterOf(MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana"));

            Result result = roster.CheckNameFree("ana");

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
        }

        [Fact]
        public void CheckNameFree_ComparesTheDisplayedNameAndNotTheStoredOne()
        {
            GroupMember account = MemberTestData.Account(MemberTestData.GroupId, GroupTestData.MemberId, "Ana");
            GroupRoster roster = new([MemberTestData.Named(account, "Ana Petrova")]);

            Result displayedName = roster.CheckNameFree("ana petrova");
            Result storedName = roster.CheckNameFree("Ana");

            ResultAssert.Failed(displayedName, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
            Assert.True(storedName.IsSuccess);
        }

        [Fact]
        public void CheckNameFree_ANameOnlyEndedRowsShow_Succeeds()
        {
            GroupRoster roster = MemberTestData.RosterOf(
                MemberTestData.RemovedPlainName(MemberTestData.GroupId, "Marko"),
                MemberTestData.Left(MemberTestData.GroupId, GroupTestData.MemberId, "Ana"),
                MemberTestData.SetAside(MemberTestData.GroupId, GroupTestData.StrangerId, "Bojan", GroupTestData.Moment.AddDays(-1)));

            Assert.True(roster.CheckNameFree("Marko").IsSuccess);
            Assert.True(roster.CheckNameFree("Ana").IsSuccess);
            Assert.True(roster.CheckNameFree("Bojan").IsSuccess);
        }

        [Fact]
        public void CheckNameFree_ANameThatOnlyPartlyMatchesAShownName_Succeeds()
        {
            GroupRoster roster = MemberTestData.RosterOf(MemberTestData.PlainName(MemberTestData.GroupId, "Marko Petrov"));

            Assert.True(roster.CheckNameFree("Marko").IsSuccess);
            Assert.True(roster.CheckNameFree("Marko Petrov Jr").IsSuccess);
        }

        [Fact]
        public void CheckNameFree_TheErrorNamesTheTakenName()
        {
            GroupRoster roster = MemberTestData.RosterOf(MemberTestData.PlainName(MemberTestData.GroupId, "Marko"));

            Result result = roster.CheckNameFree("Marko");

            Assert.Contains("Marko", result.Error, StringComparison.Ordinal);
        }
    }
}
