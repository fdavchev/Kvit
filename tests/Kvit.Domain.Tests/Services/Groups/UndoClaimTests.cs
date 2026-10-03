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
    public class UndoClaimTests
    {
        private readonly FakeGroupRepository _groups = new();
        private readonly FakeGroupMemberRepository _members = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly UndoClaim _undoClaim;
        private readonly Group _group = GroupTestData.NewGroup();
        private readonly GroupMember _ownerRow;
        private readonly GroupMember _claimedRow;

        public UndoClaimTests()
        {
            _ownerRow = MemberTestData.Account(_group.Id, GroupTestData.OwnerId, "Filip");
            _claimedRow = MemberTestData.Claimed(_group.Id, GroupTestData.MemberId, "Grandma");
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _members.Seed(_ownerRow);
            _members.Seed(_claimedRow, "Ana Petrova");
            _undoClaim = new UndoClaim(_groups, _members, _activityEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_ClaimerHasASetAsideRow_ClearsTheNameAndBringsTheSetAsideRowBack()
        {
            GroupMember setAside = MemberTestData.SetAside(_group.Id, GroupTestData.MemberId, "Ana", GroupTestData.Moment.AddDays(-1));
            _members.Seed(setAside);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, _claimedRow.Id);

            Assert.True(result.IsSuccess);
            Assert.Null(_claimedRow.UserId);
            Assert.Null(_claimedRow.ClaimedAt);
            Assert.Equal("Grandma", _claimedRow.Name);
            Assert.Null(setAside.RemovedAt);
            Assert.Null(setAside.RemovedByUserId);
            Assert.Null(setAside.EndKind);
            Assert.Empty(_members.Added);
        }

        [Fact]
        public async Task Execute_ClaimerHasSeveralSetAsideRows_BringsBackOnlyTheLatestOne()
        {
            GroupMember older = MemberTestData.SetAside(_group.Id, GroupTestData.MemberId, "Ana", GroupTestData.Moment.AddDays(-5));
            GroupMember newer = MemberTestData.SetAside(_group.Id, GroupTestData.MemberId, "Ana", GroupTestData.Moment.AddDays(-1));
            _members.Seed(older);
            _members.Seed(newer);

            await ExecuteAsync(GroupTestData.OwnerId, _claimedRow.Id);

            Assert.Null(newer.EndKind);
            Assert.Equal(MemberEndKind.SetAside, older.EndKind);
        }

        [Fact]
        public async Task Execute_ClaimerHasNoSetAsideRow_AddsANewOwnRowUnderTheirDisplayedNameAddedByTheOwner()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, _claimedRow.Id);

            Assert.True(result.IsSuccess);
            GroupMember added = Assert.Single(_members.Added);
            Assert.Equal(_group.Id, added.GroupId);
            Assert.Equal(GroupTestData.MemberId, added.UserId);
            Assert.Equal("Ana Petrova", added.Name);
            Assert.Equal(GroupTestData.OwnerId, added.AddedByUserId);
            Assert.Equal(GroupTestData.Moment, added.JoinedAt);
            Assert.Null(added.RemovedAt);
            Assert.Null(added.ClaimedAt);
        }

        [Fact]
        public async Task Execute_OnlyAnotherUsersRowWasSetAside_IgnoresItAndAddsANewRowForTheClaimer()
        {
            GroupMember otherUsersRow = MemberTestData.SetAside(_group.Id, MemberTestData.OtherUserId, "Marko", GroupTestData.Moment.AddDays(-1));
            _members.Seed(otherUsersRow);

            await ExecuteAsync(GroupTestData.OwnerId, _claimedRow.Id);

            Assert.Equal(MemberEndKind.SetAside, otherUsersRow.EndKind);
            Assert.Equal(GroupTestData.MemberId, Assert.Single(_members.Added).UserId);
        }

        [Fact]
        public async Task Execute_ClaimerHasASetAsideRow_SavesTheClearedNameBeforeTheSetAsideRowIsBroughtBack()
        {
            GroupMember setAside = MemberTestData.SetAside(_group.Id, GroupTestData.MemberId, "Ana", GroupTestData.Moment.AddDays(-1));
            _members.Seed(setAside);

            await ExecuteAsync(GroupTestData.OwnerId, _claimedRow.Id);

            IReadOnlyList<MemberSnapshot> savedState = Assert.Single(_members.SavedStates);
            MemberSnapshot targetWhenSaved = Assert.Single(savedState, row => row.Id == _claimedRow.Id);
            MemberSnapshot setAsideWhenSaved = Assert.Single(savedState, row => row.Id == setAside.Id);
            Assert.Null(targetWhenSaved.UserId);
            Assert.Null(targetWhenSaved.ClaimedAt);
            Assert.Equal(MemberEndKind.SetAside, setAsideWhenSaved.EndKind);
        }

        [Fact]
        public async Task Execute_ClaimerHasNoSetAsideRow_SavesTheClearedNameBeforeTheNewRowIsAdded()
        {
            await ExecuteAsync(GroupTestData.OwnerId, _claimedRow.Id);

            IReadOnlyList<MemberSnapshot> savedState = Assert.Single(_members.SavedStates);
            Assert.Equal(2, savedState.Count);
            MemberSnapshot targetWhenSaved = Assert.Single(savedState, row => row.Id == _claimedRow.Id);
            Assert.Null(targetWhenSaved.UserId);
            Assert.Null(targetWhenSaved.ClaimedAt);
        }

        [Fact]
        public async Task Execute_OwnerUndoesAClaim_WritesOneClaimUndoneEventWithTheClaimersDisplayedNameAndTheClaimedName()
        {
            await ExecuteAsync(GroupTestData.OwnerId, _claimedRow.Id);

            ActivityEvent activityEvent = Assert.Single(_activityEvents.Added);
            Assert.Equal(ActivityEventType.ClaimUndone, activityEvent.Type);
            Assert.Equal(_group.Id, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(_claimedRow.Id, activityEvent.MemberId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal("Ana Petrova", ActivityEventData.Name(activityEvent));
            Assert.Equal("Grandma", ActivityEventData.ClaimedName(activityEvent));
        }

        [Fact]
        public async Task Execute_ANonMember_FailsWith404GroupNotFoundAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.StrangerId, _claimedRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            AssertStillClaimed();
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwner_FailsWith403GroupNotOwnerAndChangesNothing()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, _claimedRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            AssertStillClaimed();
        }

        [Fact]
        public async Task Execute_AMemberWhoIsNotTheOwnerAndAnUnknownTarget_FailsWith403BeforeTheTargetIsLookedUp()
        {
            Result result = await ExecuteAsync(GroupTestData.MemberId, Guid.NewGuid());

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Execute_AnUnknownTarget_FailsWith404MemberNotFound()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, Guid.NewGuid());

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            AssertStillClaimed();
        }

        [Fact]
        public async Task Execute_ARemovedClaimedRow_FailsWith404MemberNotFound()
        {
            _claimedRow.Remove(GroupTestData.OwnerId, GroupTestData.OwnerId, GroupTestData.Moment.AddDays(-1));

            Result result = await ExecuteAsync(GroupTestData.OwnerId, _claimedRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Empty(_members.SavedStates);
            Assert.Empty(_activityEvents.Added);
        }

        [Fact]
        public async Task Execute_APlainNameThatWasNeverClaimed_FailsWith400MemberNotClaimed()
        {
            GroupMember plainName = MemberTestData.PlainName(_group.Id, "Marko");
            _members.Seed(plainName);

            Result result = await ExecuteAsync(GroupTestData.OwnerId, plainName.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_CLAIMED);
            AssertStillClaimed();
        }

        [Fact]
        public async Task Execute_AnOrdinaryAccountRow_FailsWith400MemberNotClaimedAndKeepsTheAccount()
        {
            Result result = await ExecuteAsync(GroupTestData.OwnerId, _ownerRow.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_CLAIMED);
            Assert.Equal(GroupTestData.OwnerId, _ownerRow.UserId);
            AssertStillClaimed();
        }

        private Task<Result> ExecuteAsync(Guid userId, Guid memberId)
        {
            return _undoClaim.Execute(_group.Id, userId, memberId, TestContext.Current.CancellationToken);
        }

        private void AssertStillClaimed()
        {
            Assert.Equal(GroupTestData.MemberId, _claimedRow.UserId);
            Assert.NotNull(_claimedRow.ClaimedAt);
            Assert.Empty(_members.Added);
            Assert.Empty(_members.SavedStates);
            Assert.Empty(_activityEvents.Added);
        }
    }
}
