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
    public class UndoInviteResetTests
    {
        private const string ResetToken = "token-after-the-reset";

        private static readonly DateTimeOffset ResetAt = GroupTestData.Moment.AddHours(-1);

        private readonly FakeGroupRepository _groups = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly UndoInviteReset _undoInviteReset;
        private readonly Group _group = GroupTestData.NewGroup();

        public UndoInviteResetTests()
        {
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _undoInviteReset = new UndoInviteReset(_groups, _activityEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_OwnerAfterAReset_ReturnsAndRestoresTheOldTokenAndClearsThePreviousOne()
        {
            _group.ResetInvite(ResetToken, ResetAt);

            Result<string> result = await ExecuteAsync(GroupTestData.OwnerId);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.InviteToken, result.Value);
            Assert.Equal(GroupTestData.InviteToken, _group.InviteToken);
            Assert.Null(_group.PreviousInviteToken);
            Assert.Equal(GroupTestData.Moment, _group.InviteTokenCreatedAt);
        }

        [Fact]
        public async Task Execute_OwnerAfterAReset_WritesOneInviteLinkRestoredEventThatCarriesNoToken()
        {
            _group.ResetInvite(ResetToken, ResetAt);

            await ExecuteAsync(GroupTestData.OwnerId);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.InviteLinkRestored, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Null(activityEvent.Data);
            Assert.Null(activityEvent.Changes);
        }

        [Fact]
        public async Task Execute_OwnerOnAGroupThatWasNeverReset_FailsWith400InviteNothingToUndoAndWritesNothing()
        {
            Result<string> result = await ExecuteAsync(GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.INVITE_NOTHING_TO_UNDO);
            Assert.Equal(GroupTestData.InviteToken, _group.InviteToken);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_OwnerTwiceAfterOneReset_FailsTheSecondTimeAndWritesOnlyOneEvent()
        {
            _group.ResetInvite(ResetToken, ResetAt);
            await ExecuteAsync(GroupTestData.OwnerId);

            Result<string> second = await ExecuteAsync(GroupTestData.OwnerId);

            ResultAssert.Failed(second, HttpStatusCode.BadRequest, ResultCodes.INVITE_NOTHING_TO_UNDO);
            Assert.Equal(GroupTestData.InviteToken, _group.InviteToken);
            Assert.Single(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_ANonMember_FailsWith404GroupNotFoundAndChangesNothing()
        {
            _group.ResetInvite(ResetToken, ResetAt);

            Result<string> result = await ExecuteAsync(GroupTestData.StrangerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(ResetToken, _group.InviteToken);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwner_FailsWith403GroupNotOwnerAndChangesNothing()
        {
            _group.ResetInvite(ResetToken, ResetAt);

            Result<string> result = await ExecuteAsync(GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(ResetToken, _group.InviteToken);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwnerOnAGroupThatWasNeverReset_FailsWith403BeforeTheNothingToUndoRule()
        {
            Result<string> result = await ExecuteAsync(GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Execute_ADeletedGroup_FailsWith404GroupNotFound()
        {
            _group.ResetInvite(ResetToken, ResetAt);
            _group.Delete(GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-1));

            Result<string> result = await ExecuteAsync(GroupTestData.OwnerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(ResetToken, _group.InviteToken);
        }

        private Task<Result<string>> ExecuteAsync(Guid userId)
        {
            return _undoInviteReset.Execute(_group.Id, userId, TestContext.Current.CancellationToken);
        }
    }
}
