using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupInviteTests
    {
        private const string NewToken = "invite-token-2";
        private const string ThirdToken = "invite-token-3";

        private static readonly DateTimeOffset ResetAt = GroupTestData.Moment.AddHours(1);
        private static readonly DateTimeOffset UndoneAt = GroupTestData.Moment.AddHours(2);

        [Fact]
        public void NewGroup_HasNoPreviousInviteToken()
        {
            Group group = GroupTestData.NewGroup();

            Assert.Null(group.PreviousInviteToken);
        }

        [Fact]
        public void ResetInvite_MakesTheNewTokenCurrentAndKeepsTheOldOneAsPrevious()
        {
            Group group = GroupTestData.NewGroup();

            group.ResetInvite(NewToken, ResetAt);

            Assert.Equal(NewToken, group.InviteToken);
            Assert.Equal(GroupTestData.InviteToken, group.PreviousInviteToken);
            Assert.Equal(ResetAt, group.InviteTokenCreatedAt);
        }

        [Fact]
        public void ResetInvite_TwiceInARow_KeepsOnlyTheTokenBeforeTheLastResetAsPrevious()
        {
            Group group = GroupTestData.NewGroup();
            group.ResetInvite(NewToken, ResetAt);

            group.ResetInvite(ThirdToken, UndoneAt);

            Assert.Equal(ThirdToken, group.InviteToken);
            Assert.Equal(NewToken, group.PreviousInviteToken);
            Assert.Equal(UndoneAt, group.InviteTokenCreatedAt);
        }

        [Fact]
        public void UndoInviteReset_AfterAReset_BringsTheOldTokenBackAndClearsThePreviousOne()
        {
            Group group = GroupTestData.NewGroup();
            group.ResetInvite(NewToken, ResetAt);

            Result result = group.UndoInviteReset(UndoneAt);

            Assert.True(result.IsSuccess);
            Assert.Equal(GroupTestData.InviteToken, group.InviteToken);
            Assert.Null(group.PreviousInviteToken);
            Assert.Equal(UndoneAt, group.InviteTokenCreatedAt);
        }

        [Fact]
        public void UndoInviteReset_OnAGroupThatWasNeverReset_FailsWith400InviteNothingToUndoAndChangesNothing()
        {
            Group group = GroupTestData.NewGroup();

            Result result = group.UndoInviteReset(UndoneAt);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.INVITE_NOTHING_TO_UNDO);
            Assert.Equal(GroupTestData.InviteToken, group.InviteToken);
            Assert.Equal(GroupTestData.Moment, group.InviteTokenCreatedAt);
        }

        [Fact]
        public void UndoInviteReset_TwiceInARow_FailsTheSecondTimeBecauseThePreviousTokenIsGone()
        {
            Group group = GroupTestData.NewGroup();
            group.ResetInvite(NewToken, ResetAt);
            group.UndoInviteReset(UndoneAt);

            Result result = group.UndoInviteReset(UndoneAt.AddHours(1));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.INVITE_NOTHING_TO_UNDO);
            Assert.Equal(GroupTestData.InviteToken, group.InviteToken);
        }

        [Fact]
        public void InviteNotFound_Answers404InviteNotFound()
        {
            Result<Group> result = Group.InviteNotFound<Group>();

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
        }
    }
}
