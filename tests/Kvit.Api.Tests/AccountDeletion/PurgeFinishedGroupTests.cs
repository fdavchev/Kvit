using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeFinishedGroupTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Purge_OwnerAloneInTheGroup_FinishesTheGroupAtThePurgeTime()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);

            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
            Assert.Equal(now, await DeletionRows.TimeOfAsync(_app, "groups WHERE id = @id", "finished_at", new NpgsqlParameter("id", group.GroupId)));
        }

        [Fact]
        public async Task Purge_OwnerAloneInTheGroup_WritesOneGroupFinishedEventByTheDeletedPerson()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);

            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
            Assert.Equal(owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "GroupFinished", "actor_user_id"));
            Assert.Equal(now, await DeletionRows.TimeOfAsync(_app, "activity_events WHERE group_id = @id AND type = 'GroupFinished'", "created_at", new NpgsqlParameter("id", group.GroupId)));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "OwnershipTransferred"));
        }

        [Fact]
        public async Task Purge_OwnerAloneInTheGroup_TheTombstoneStaysTheOwner()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(owner.UserId, await DeletionRows.OwnerOfAsync(_app, group.GroupId));
            Assert.Equal(1, await DeletionRows.CountUsersWithIdAsync(_app, owner.UserId));
        }

        [Fact]
        public async Task Purge_OwnerWithOnlyPlainNames_FinishesTheGroup()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddPlainNameAsync(_app, group, "Marko", 5);
            await DeletionGroups.AddPlainNameAsync(_app, group, "Cveta", 6);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
            Assert.Equal(owner.UserId, await DeletionRows.OwnerOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Purge_OwnerWithOnlyGuests_FinishesTheGroup()
        {
            ExpenseGroup expenseGroup = await _app.CreateExpenseGroupAsync("Marko");
            await GuestSteps.TapFreeNameAsync(_app, expenseGroup.GroupId, expenseGroup.PlainRowIds[0]);
            await GroupRows.EndMembershipAsync(_app, expenseGroup.GroupId, expenseGroup.Member.UserId, "Left");

            await DeletionSteps.RequestAndPurgeAsync(_app, expenseGroup.Owner.Client, expenseGroup.Owner.UserId, DeletionRequests.KeepName);

            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_app, expenseGroup.GroupId, "status"));
            Assert.Equal(expenseGroup.Owner.UserId, await DeletionRows.OwnerOfAsync(_app, expenseGroup.GroupId));
        }

        [Fact]
        public async Task Purge_OwnerWhoseOnlyOtherMemberAlreadyRequestedDeletion_FinishesTheGroup()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser leaving = await _app.SignUpAsync("Leaving");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, group, leaving, 5);
            await DeletionSteps.RequestAsync(leaving.Client, DeletionRequests.KeepName);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
        }

        [Fact]
        public async Task Purge_OwnerWhoseOnlyOtherMembersEndedTheirMembership_FinishesTheGroup()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser gone = await _app.SignUpAsync("Gone");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddEndedAccountAsync(_app, group, gone, "Left", 5);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
        }

        [Fact]
        public async Task Purge_OwnerOfAClosingGroupWithNoHeir_FinishesTheGroupAtThePurgeTime()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await _app.ExecuteAsync(
                "UPDATE groups SET status = 'Closing', closing_started_at = @started WHERE id = @id",
                new NpgsqlParameter("id", group.GroupId),
                new NpgsqlParameter("started", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = ExpensesApp.ClockStart });

            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
            Assert.Equal(now, await DeletionRows.TimeOfAsync(_app, "groups WHERE id = @id", "finished_at", new NpgsqlParameter("id", group.GroupId)));
        }

        [Fact]
        public async Task Purge_OwnerOfAnAlreadyFinishedGroupWithNoHeir_ChangesNothingInThatGroup()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await _app.ExecuteAsync(
                "UPDATE groups SET status = 'Finished', finished_at = @finished WHERE id = @id",
                new NpgsqlParameter("id", group.GroupId),
                new NpgsqlParameter("finished", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = ExpensesApp.ClockStart });
            string groupBefore = Assert.Single(await _app.QueryAsync("SELECT g::text FROM groups g WHERE g.id = @id", new NpgsqlParameter("id", group.GroupId)))!;
            int eventsBefore = await GroupRows.CountEventsAsync(_app, group.GroupId);

            await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(groupBefore, Assert.Single(await _app.QueryAsync("SELECT g::text FROM groups g WHERE g.id = @id", new NpgsqlParameter("id", group.GroupId))));
            Assert.Equal(eventsBefore, await GroupRows.CountEventsAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Purge_FinishedGroupWithTheTombstoneOwner_ARemainingGuestCannotUnlockIt()
        {
            ExpenseGroup expenseGroup = await _app.CreateExpenseGroupAsync("Marko");
            GuestMember guest = await GuestSteps.TapFreeNameAsync(_app, expenseGroup.GroupId, expenseGroup.PlainRowIds[0]);
            await GroupRows.EndMembershipAsync(_app, expenseGroup.GroupId, expenseGroup.Member.UserId, "Left");
            await DeletionSteps.RequestAndPurgeAsync(_app, expenseGroup.Owner.Client, expenseGroup.Owner.UserId, DeletionRequests.KeepName);

            HttpResponseMessage response = await guest.Client.PostAsync($"/api/groups/{expenseGroup.GroupId}/unlock", null, TestContext.Current.CancellationToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_app, expenseGroup.GroupId, "status"));
        }
    }
}
