using System.Net;
using System.Text.Json;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;
using Kvit.Domain.Tests.Entities;
using Kvit.Domain.Tests.Fakes;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Services.Groups
{
    public class CreateGroupTests
    {
        private const string OwnerAccountName = "Ana";
        private const string GeneratedToken = "token-from-the-generator";

        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly FakeUsageEventRepository _usageEvents = new();
        private readonly CreateGroup _createGroup;

        public CreateGroupTests()
        {
            _createGroup = new CreateGroup(
                _groups,
                _members,
                _activityEvents,
                _usageEvents,
                new FakeInviteTokenGenerator(GeneratedToken),
                new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public void Execute_ValidInput_ReturnsTheIdOfTheAddedGroup()
        {
            Result<Guid> result = Execute(GroupTestData.ValidName, GroupTestData.ValidEmoji, "EUR");

            Assert.True(result.IsSuccess);
            Group group = Assert.Single(_groups.Added);
            Assert.Equal(group.Id, result.Value);
            Assert.Equal(GroupTestData.ValidName, group.Name);
            Assert.Equal(GroupTestData.ValidEmoji, group.Emoji);
            Assert.Equal(Currency.EUR, group.DefaultCurrency);
            Assert.Equal(GroupTestData.OwnerId, group.OwnerUserId);
            Assert.Equal(GroupTestData.Moment, group.CreatedAt);
        }

        [Fact]
        public void Execute_ValidInput_TakesTheInviteTokenFromTheGenerator()
        {
            Execute(GroupTestData.ValidName, GroupTestData.ValidEmoji, GroupTestData.ValidCurrency);

            Group group = Assert.Single(_groups.Added);
            Assert.Equal(GeneratedToken, group.InviteToken);
        }

        [Fact]
        public void Execute_ValidInput_AddsTheOwnerAsAMemberUnderTheirAccountName()
        {
            Execute(GroupTestData.ValidName, GroupTestData.ValidEmoji, GroupTestData.ValidCurrency);

            Group group = Assert.Single(_groups.Added);
            GroupMember member = Assert.Single(_members.Added);
            Assert.Equal(group.Id, member.GroupId);
            Assert.Equal(GroupTestData.OwnerId, member.UserId);
            Assert.Equal(OwnerAccountName, member.Name);
            Assert.Equal(GroupTestData.OwnerId, member.AddedByUserId);
            Assert.Equal(GroupTestData.Moment, member.JoinedAt);
        }

        [Fact]
        public void Execute_ValidInput_WritesOneGroupCreatedActivityEventWithTheTrimmedName()
        {
            Execute("  Greece trip  ", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency);

            Group group = Assert.Single(_groups.Added);
            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.GroupCreated, activityEvent.Type);
            Assert.Equal(group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.NotNull(activityEvent.Data);
            Assert.Equal("Greece trip", JsonDocument.Parse(activityEvent.Data).RootElement.GetProperty("name").GetString());
        }

        [Fact]
        public void Execute_ValidInput_WritesOneGroupCreatedUsageEventForTheOwner()
        {
            Execute(GroupTestData.ValidName, GroupTestData.ValidEmoji, GroupTestData.ValidCurrency);

            UsageEvent usageEvent = Assert.Single(_usageEvents.Added);
            Assert.Equal(UsageEventType.GroupCreated, usageEvent.Type);
            Assert.Equal(GroupTestData.OwnerId, usageEvent.UserId);
            Assert.Equal(GroupTestData.Moment, usageEvent.OccurredAt);
        }

        [Theory]
        [InlineData("", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency, ResultCodes.GROUP_NAME_INVALID)]
        [InlineData(GroupTestData.ValidName, "", GroupTestData.ValidCurrency, ResultCodes.GROUP_EMOJI_INVALID)]
        [InlineData(GroupTestData.ValidName, GroupTestData.ValidEmoji, "USD", ResultCodes.GROUP_CURRENCY_INVALID)]
        public void Execute_InvalidInput_FailsWithTheErrorCodeAndAddsNothing(string name, string emoji, string currency, string expectedErrorCode)
        {
            Result<Guid> result = Execute(name, emoji, currency);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Empty(_groups.Added);
            Assert.Empty(_members.Added);
            Assert.Empty(_activityEvents.Added);
            Assert.Empty(_usageEvents.Added);
        }

        private Result<Guid> Execute(string name, string emoji, string currency)
        {
            return _createGroup.Execute(GroupTestData.OwnerId, OwnerAccountName, name, emoji, currency);
        }
    }
}
