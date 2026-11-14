using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeOwnershipTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Purge_OwnerWithTwoMembers_PassesOwnershipToTheEarliestJoinedOne()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser later = await _app.SignUpAsync("Later");
            SignedInUser earlier = await _app.SignUpAsync("Earlier");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, group, later, 20);
            await DeletionGroups.AddAccountAsync(_app, group, earlier, 10);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(earlier.UserId, await DeletionRows.OwnerOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Purge_OwnerWithTwoMembersJoinedAtTheSameMoment_PassesOwnershipToTheLowestMemberId()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser first = await _app.SignUpAsync("First");
            SignedInUser second = await _app.SignUpAsync("Second");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, group, first, 10);
            await DeletionGroups.AddAccountAsync(_app, group, second, 10);
            string lowestUserId = Assert.Single(await _app.QueryAsync(
                "SELECT user_id::text FROM group_members WHERE group_id = @group_id AND user_id <> @owner ORDER BY joined_at, id LIMIT 1",
                new NpgsqlParameter("group_id", group.GroupId),
                new NpgsqlParameter("owner", owner.UserId)))!;

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(Guid.Parse(lowestUserId), await DeletionRows.OwnerOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Purge_OwnerWithAPlainNameJoinedEarlier_SkipsThePlainName()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser member = await _app.SignUpAsync("Bojan");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddPlainNameAsync(_app, group, "Marko", 5);
            await DeletionGroups.AddAccountAsync(_app, group, member, 10);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(member.UserId, await DeletionRows.OwnerOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Purge_OwnerWithAGuestJoinedEarlier_SkipsTheGuest()
        {
            ExpenseGroup expenseGroup = await _app.CreateExpenseGroupAsync("Marko");
            GuestMember guest = await GuestSteps.TapFreeNameAsync(_app, expenseGroup.GroupId, expenseGroup.PlainRowIds[0]);
            await DeletionRows.SetJoinedAtAsync(_app, guest.RowId, DeletionGroups.JoinedAt(-5));

            await DeletionSteps.RequestAndPurgeAsync(_app, expenseGroup.Owner.Client, expenseGroup.Owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(expenseGroup.Member.UserId, await DeletionRows.OwnerOfAsync(_app, expenseGroup.GroupId));
        }

        [Fact]
        public async Task Purge_OwnerWithAMemberWhoAlreadyRequestedTheirOwnDeletion_SkipsThatMember()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser leaving = await _app.SignUpAsync("Leaving");
            SignedInUser staying = await _app.SignUpAsync("Staying");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, group, leaving, 5);
            await DeletionGroups.AddAccountAsync(_app, group, staying, 10);
            await DeletionSteps.RequestAsync(leaving.Client, DeletionRequests.KeepName);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(staying.UserId, await DeletionRows.OwnerOfAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        [InlineData("SetAside")]
        public async Task Purge_OwnerWithAnEndedMembershipJoinedEarlier_SkipsTheEndedMember(string endKind)
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser gone = await _app.SignUpAsync("Gone");
            SignedInUser active = await _app.SignUpAsync("Active");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddEndedAccountAsync(_app, group, gone, endKind, 5);
            await DeletionGroups.AddAccountAsync(_app, group, active, 10);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(active.UserId, await DeletionRows.OwnerOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Purge_OwnerHandsOver_WritesAnOwnershipTransferredEventForTheNewOwnersRow()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser heir = await _app.SignUpAsync("Heir");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            Guid heirRowId = await DeletionGroups.AddAccountAsync(_app, group, heir, 10);

            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "OwnershipTransferred"));
            Assert.Equal(owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "OwnershipTransferred", "actor_user_id"));
            Assert.Equal(heirRowId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "OwnershipTransferred", "member_id"));
            Assert.Equal("Heir", await GroupRows.EventValueAsync(_app, group.GroupId, "OwnershipTransferred", "data->>'name'"));
            Assert.Equal(now, await DeletionRows.TimeOfAsync(_app, "activity_events WHERE group_id = @group_id AND type = 'OwnershipTransferred'", "created_at", new NpgsqlParameter("group_id", group.GroupId)));
        }

        [Fact]
        public async Task Purge_OwnerHandsOver_WritesNoGroupFinishedEventAndKeepsTheGroupOpen()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser heir = await _app.SignUpAsync("Heir");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, group, heir, 10);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
            Assert.Equal("Open", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
        }

        [Fact]
        public async Task Purge_OwnerHandsOver_TheNewOwnerCanUseTheOwnerTools()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser heir = await _app.SignUpAsync("Heir");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, group, heir, 10);
            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            HttpResponseMessage rename = await GroupRequests.UpdateAsync(heir.Client, group.GroupId, name: "Renamed by the heir");

            Assert.Equal(HttpStatusCode.NoContent, rename.StatusCode);
        }

        [Fact]
        public async Task Purge_OwnerHandsOver_TheNewOwnerSeesThemselvesAsOwnerInTheMemberList()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser heir = await _app.SignUpAsync("Heir");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            Guid heirRowId = await DeletionGroups.AddAccountAsync(_app, group, heir, 10);
            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            JsonElement row = await GroupRequests.MemberRowAsync(heir.Client, group.GroupId, heirRowId);

            Assert.True(row.GetProperty("isOwner").GetBoolean());
        }

        [Fact]
        public async Task Purge_PersonOwningTwoGroups_HandsOverBothAndFinishesTheOneWithNoHeir()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser heir = await _app.SignUpAsync("Heir");
            DeletionGroup withHeir = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, withHeir, heir, 10);
            DeletionGroup alone = await DeletionGroups.CreateAsync(_app, owner);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(heir.UserId, await DeletionRows.OwnerOfAsync(_app, withHeir.GroupId));
            Assert.Equal("Open", await GroupRows.GroupValueAsync(_app, withHeir.GroupId, "status"));
            Assert.Equal(owner.UserId, await DeletionRows.OwnerOfAsync(_app, alone.GroupId));
            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_app, alone.GroupId, "status"));
        }

        [Fact]
        public async Task Purge_OwnerOfAGroupThatWasDeleted_StillPassesOwnership()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser heir = await _app.SignUpAsync("Heir");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, group, heir, 10);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, owner.UserId, 1);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(heir.UserId, await DeletionRows.OwnerOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Purge_OwnerOfAFinishedGroupWithAnHeir_PassesOwnershipAndTheGroupStaysFinished()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser heir = await _app.SignUpAsync("Heir");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, group, heir, 10);
            await GroupRows.SetStatusAsync(_app, group.GroupId, "Finished");

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(heir.UserId, await DeletionRows.OwnerOfAsync(_app, group.GroupId));
            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
        }

        [Fact]
        public async Task Purge_PersonWhoOnlyBelongsToAGroup_LeavesItsOwnerAndStatusAlone()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser member = await _app.SignUpAsync("Bojan");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, group, member, 10);
            string groupBefore = Assert.Single(await _app.QueryAsync("SELECT g::text FROM groups g WHERE g.id = @id", new NpgsqlParameter("id", group.GroupId)))!;

            await DeletionSteps.RequestAndPurgeAsync(_app, member.Client, member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(groupBefore, Assert.Single(await _app.QueryAsync("SELECT g::text FROM groups g WHERE g.id = @id", new NpgsqlParameter("id", group.GroupId))));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "OwnershipTransferred"));
        }

        [Fact]
        public async Task Purge_FormerMemberOwner_PassesOwnershipJustLikeKeepName()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser heir = await _app.SignUpAsync("Heir");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, group, heir, 10);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.FormerMember);

            Assert.Equal(heir.UserId, await DeletionRows.OwnerOfAsync(_app, group.GroupId));
        }
    }
}
