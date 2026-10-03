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
    public class AddMemberTests
    {
        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly AddMember _addMember;
        private readonly Group _group = GroupTestData.NewGroup();

        public AddMemberTests()
        {
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _members.Seed(MemberTestData.Account(_group.Id, GroupTestData.OwnerId, "Filip"));
            _members.Seed(MemberTestData.Account(_group.Id, GroupTestData.MemberId, "Ana"), "Ana Petrova");
            _members.Seed(MemberTestData.PlainName(_group.Id, "Marko"));
            _members.Seed(MemberTestData.RemovedPlainName(_group.Id, "Aunt"));
            _addMember = new AddMember(_groups, _members, _activityEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Execute_AnyCurrentMember_AddsAPlainNameRowAddedByThem(bool isOwner)
        {
            Guid caller = isOwner ? GroupTestData.OwnerId : GroupTestData.MemberId;

            Result<Guid> result = await ExecuteAsync(caller, "Grandma");

            Assert.True(result.IsSuccess);
            GroupMember added = Assert.Single(_members.Added);
            Assert.Equal(added.Id, result.Value);
            Assert.Equal(_group.Id, added.GroupId);
            Assert.Null(added.UserId);
            Assert.Equal("Grandma", added.Name);
            Assert.Equal(caller, added.AddedByUserId);
            Assert.Equal(GroupTestData.Moment, added.JoinedAt);
        }

        [Fact]
        public async Task Execute_NameWithSpacesAround_AddsTheTrimmedName()
        {
            await ExecuteAsync(GroupTestData.MemberId, "  Grandma  ");

            Assert.Equal("Grandma", Assert.Single(_members.Added).Name);
        }

        [Fact]
        public async Task Execute_ValidName_WritesOneMemberAddedEventNamingTheNewRowAndTheCaller()
        {
            await ExecuteAsync(GroupTestData.MemberId, "Grandma");

            GroupMember added = Assert.Single(_members.Added);
            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.MemberAdded, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.MemberId, activityEvent.ActorUserId);
            Assert.Equal(added.Id, activityEvent.MemberId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal("Grandma", ActivityEventData.Name(activityEvent));
        }

        [Fact]
        public async Task Execute_NameOfAPersonWhoWasRemoved_IsFreeAndAdded()
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.MemberId, "Aunt");

            Assert.True(result.IsSuccess);
            Assert.Equal("Aunt", Assert.Single(_members.Added).Name);
        }

        [Fact]
        public async Task Execute_ANonMember_FailsWith404GroupNotFoundAndWritesNothing()
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, "Grandma");

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_AGroupThatDoesNotExist_FailsWith404GroupNotFound()
        {
            Result<Guid> result = await _addMember.Execute(Guid.NewGuid(), GroupTestData.OwnerId, "Grandma", TestContext.Current.CancellationToken);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_ADeletedGroup_FailsWith404GroupNotFound()
        {
            _group.Delete(GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-1));

            Result<Guid> result = await ExecuteAsync(GroupTestData.OwnerId, "Grandma");

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_ANonMemberWithAnInvalidName_FailsWith404BeforeTheNameIsChecked()
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.StrangerId, string.Empty);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Execute_EmptyOrBlankName_FailsWith400MemberNameInvalidAndWritesNothing(string name)
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.MemberId, name);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_INVALID);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_NameLongerThanTheLimit_FailsWith400MemberNameInvalidAndWritesNothing()
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.MemberId, new string('a', AccountRules.DisplayNameMaxLength + 1));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_INVALID);
            AssertNothingWritten();
        }

        [Theory]
        [InlineData("Marko")]
        [InlineData("MARKO")]
        [InlineData("  marko  ")]
        [InlineData("Filip")]
        [InlineData("Ana Petrova")]
        public async Task Execute_ANameACurrentMemberAlreadyShows_FailsWith400MemberNameTakenAndWritesNothing(string name)
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.MemberId, name);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
            AssertNothingWritten();
        }

        [Fact]
        public async Task Execute_TheStoredNameOfAnAccountWhoseDisplayedNameDiffers_IsFree()
        {
            Result<Guid> result = await ExecuteAsync(GroupTestData.MemberId, "Ana");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task Execute_TheSameNameTwice_AddsItOnceAndRefusesTheSecondTime()
        {
            Result<Guid> first = await ExecuteAsync(GroupTestData.MemberId, "Grandma");
            Result<Guid> second = await ExecuteAsync(GroupTestData.OwnerId, "grandma");

            Assert.True(first.IsSuccess);
            ResultAssert.Failed(second, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
            Assert.Single(_members.Added);
            Assert.Single(_activityEvents.Added);
        }

        private Task<Result<Guid>> ExecuteAsync(Guid userId, string name)
        {
            return _addMember.Execute(_group.Id, userId, name, TestContext.Current.CancellationToken);
        }

        private void AssertNothingWritten()
        {
            Assert.Empty(_members.Added);
            Assert.Empty(_activityEvents.Added);
        }
    }
}
