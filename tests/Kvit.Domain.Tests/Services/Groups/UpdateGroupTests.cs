using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;
using Kvit.Domain.Tests.Entities;
using Kvit.Domain.Tests.Fakes;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Services.Groups
{
    public class UpdateGroupTests
    {
        private const string NewEmoji = "\U0001F37D";

        private readonly FakeGroupRepository _groups = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly UpdateGroup _updateGroup;
        private readonly Group _group = GroupTestData.NewGroup();

        public UpdateGroupTests()
        {
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _updateGroup = new UpdateGroup(_groups, _activityEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_NameOnly_ChangesTheNameAndWritesOneGroupRenamedEvent()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, "Rome", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency);

            Assert.True(result.IsSuccess);
            Assert.Equal("Rome", _group.Name);
            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.GroupRenamed, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
        }

        [Fact]
        public async Task Execute_EmojiAndCurrency_WritesOneGroupSettingsChangedEvent()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, GroupTestData.ValidName, NewEmoji, "EUR");

            Assert.True(result.IsSuccess);
            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.GroupSettingsChanged, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
        }

        [Fact]
        public async Task Execute_NameAndSettings_WritesOneGroupRenamedAndOneGroupSettingsChangedEvent()
        {
            await ExecuteAsync(GroupTestData.OwnerId, "Rome", NewEmoji, GroupTestData.ValidCurrency);

            ActivityEventType[] types = [.. _activityEvents.Added.Select(activityEvent => activityEvent.Type)];
            Assert.Equal([ActivityEventType.GroupRenamed, ActivityEventType.GroupSettingsChanged], types);
        }

        [Fact]
        public async Task Execute_NothingChanged_SucceedsAndWritesNoEvent()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, GroupTestData.ValidName, GroupTestData.ValidEmoji, GroupTestData.ValidCurrency);

            Assert.True(result.IsSuccess);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_UnknownGroup_FailsWith404GroupNotFound()
        {
            Result result = await _updateGroup.Execute(Guid.CreateVersion7(), GroupTestData.OwnerId, "Rome", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency, TestContext.Current.CancellationToken);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Execute_NonMemberWithInvalidInput_FailsWith404BeforeTheInputIsChecked()
        {
            Result result = await ExecuteAsync(GroupTestData.StrangerId, "", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_MemberWhoIsNotTheOwnerWithInvalidInput_FailsWith403BeforeTheInputIsChecked()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, "", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_MemberWhoIsNotTheOwner_ChangesNothingAndWritesNoEvent()
        {
            await ExecuteAsync(GroupTestData.MemberId, "Rome", NewEmoji, "EUR");

            Assert.Equal(GroupTestData.ValidName, _group.Name);
            Assert.Equal(GroupTestData.ValidEmoji, _group.Emoji);
            Assert.Empty(_activityEvents.Added);
        }

        [Theory]
        [InlineData("", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency, ResultCodes.GROUP_NAME_INVALID)]
        [InlineData("Rome", "", GroupTestData.ValidCurrency, ResultCodes.GROUP_EMOJI_INVALID)]
        [InlineData("Rome", GroupTestData.ValidEmoji, "USD", ResultCodes.GROUP_CURRENCY_INVALID)]
        public async Task Execute_OwnerWithInvalidInput_FailsWith400ChangesNothingAndWritesNoEvent(string name, string emoji, string currency, string expectedErrorCode)
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, name, emoji, currency);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(GroupTestData.ValidName, _group.Name);
            Assert.Empty(_activityEvents.Added);
        }

        private Task<Result> ExecuteAsync(Guid userId, string name, string emoji, string currency)
        {
            return _updateGroup.Execute(_group.Id, userId, name, emoji, currency, TestContext.Current.CancellationToken);
        }
    }
}
