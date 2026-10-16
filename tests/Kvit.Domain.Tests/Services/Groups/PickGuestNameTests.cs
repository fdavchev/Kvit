using System.Net;
using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;
using Kvit.Domain.Tests.Entities;
using Kvit.Domain.Tests.Fakes;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Services.Groups
{
    public class PickGuestNameTests
    {
        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly PickGuestName _pickGuestName;
        private readonly Group _group = GroupTestData.NewGroup();
        private readonly GroupMember _plainNameRow;

        public PickGuestNameTests()
        {
            _plainNameRow = MemberTestData.PlainName(_group.Id, "Grandma");
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _members.Seed(MemberTestData.Account(_group.Id, GroupTestData.OwnerId, "Filip"));
            _members.Seed(MemberTestData.Account(_group.Id, GroupTestData.MemberId, "Ana"), "Ana Petrovska");
            _members.Seed(_plainNameRow);
            _pickGuestName = new PickGuestName(_groups, _members);
        }

        [Fact]
        public async Task Execute_AFreeNameToClaim_ReturnsThatName()
        {
            Result<string> result = await ExecuteAsync(ClaimOf(_plainNameRow.Id));

            Assert.True(result.IsSuccess);
            Assert.Equal("Grandma", result.Value);
        }

        [Fact]
        public async Task Execute_AnUnknownClaimTarget_FailsWith404MemberNotFound()
        {
            Result<string> result = await ExecuteAsync(ClaimOf(Guid.NewGuid()));

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Execute_AnAccountRowAsClaimTarget_FailsWith404MemberNotFound()
        {
            GroupMember accountRow = MemberTestData.Account(_group.Id, MemberTestData.OtherUserId, "Dana");
            _members.Seed(accountRow);

            Result<string> result = await ExecuteAsync(ClaimOf(accountRow.Id));

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Execute_AnAlreadyClaimedNameAsClaimTarget_FailsWith404MemberNotFound()
        {
            GroupMember claimed = MemberTestData.Claimed(_group.Id, MemberTestData.OtherUserId, "Marko");
            _members.Seed(claimed);

            Result<string> result = await ExecuteAsync(ClaimOf(claimed.Id));

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Execute_ARemovedPlainNameAsClaimTarget_FailsWith404MemberNotFound()
        {
            GroupMember removed = MemberTestData.RemovedPlainName(_group.Id, "Aunt");
            _members.Seed(removed);

            Result<string> result = await ExecuteAsync(ClaimOf(removed.Id));

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Execute_ANewNameNobodyHas_ReturnsThatName()
        {
            Result<string> result = await ExecuteAsync(NewNameOf("Cvetan"));

            Assert.True(result.IsSuccess);
            Assert.Equal("Cvetan", result.Value);
        }

        [Theory]
        [InlineData("Ana Petrovska")]
        [InlineData("ana petrovska")]
        [InlineData("FILIP")]
        public async Task Execute_ANewNameEqualToTheNameOfAPersonWithAnAccount_FailsWith400MemberNameTaken(string name)
        {
            Result<string> result = await ExecuteAsync(NewNameOf(name));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
        }

        [Fact]
        public async Task Execute_ANewNameEqualToAFreeName_FailsWith400MemberNameTaken()
        {
            Result<string> result = await ExecuteAsync(NewNameOf("grandma"));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
        }

        [Fact]
        public async Task Execute_ANewNameEqualToTheNameOfSomeoneWhoWasRemoved_ReturnsThatName()
        {
            _members.Seed(MemberTestData.Removed(_group.Id, MemberTestData.OtherUserId, "Dana"));

            Result<string> result = await ExecuteAsync(NewNameOf("Dana"));

            Assert.True(result.IsSuccess);
            Assert.Equal("Dana", result.Value);
        }

        [Theory]
        [InlineData("no-such-token")]
        [InlineData("")]
        public async Task Execute_AnUnknownToken_FailsWith404InviteNotFound(string token)
        {
            Result<string> result = await _pickGuestName.Execute(token, NewNameOf("Cvetan"), TestContext.Current.CancellationToken);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
        }

        [Fact]
        public async Task Execute_TheTokenOfADeletedGroup_FailsWith404InviteNotFound()
        {
            _group.Delete(GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-1));

            Result<string> result = await ExecuteAsync(ClaimOf(_plainNameRow.Id));

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
        }

        private Task<Result<string>> ExecuteAsync(GuestJoinChoice choice)
        {
            return _pickGuestName.Execute(GroupTestData.InviteToken, choice, TestContext.Current.CancellationToken);
        }

        private static GuestJoinChoice ClaimOf(Guid memberId)
        {
            return GuestJoinChoice.Create(memberId, null, "Europe/Skopje", "en").Value;
        }

        private static GuestJoinChoice NewNameOf(string name)
        {
            return GuestJoinChoice.Create(null, name, "Europe/Skopje", "en").Value;
        }
    }
}
