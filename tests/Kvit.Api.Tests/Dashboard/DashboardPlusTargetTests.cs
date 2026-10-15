using System.Text.Json;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Dashboard
{
    public class DashboardPlusTargetTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task PlusTarget_UserInNoGroup_IsNull()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await DashboardRequests.ReadAsync(user);

            Assert.Equal(JsonValueKind.Null, body.GetProperty("plusTarget").ValueKind);
        }

        [Fact]
        public async Task PlusTarget_Group_AnswersExactlyTheGroupIdField()
        {
            SignedInUser ana = await _app.SignUpAsync();
            await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(DashboardJson.PlusTargetFieldNames, GroupRequests.PropertyNamesOf(body.GetProperty("plusTarget")));
        }

        [Fact]
        public async Task PlusTarget_NoExpenseYet_IsTheGroupCreatedLastByTheCaller()
        {
            SignedInUser ana = await _app.SignUpAsync();
            await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Older");
            ExpenseGroup newer = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Newer");

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(newer.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_NoExpenseYet_IsTheGroupTheCallerJoinedLastEvenWhenAnotherOneWasCreatedEarlierByThem()
        {
            SignedInUser ana = await _app.SignUpAsync();
            SignedInUser bojan = await _app.SignUpAsync(GroupsApp.MemberName);
            await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Own");
            ExpenseGroup joined = await DashboardSteps.CreateGroupAsync(_app, bojan, ana, "Joined");

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(joined.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_ExpenseAddedByTheCaller_BeatsAGroupJoinedLater()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup older = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Older");
            await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Newer");
            await DashboardSteps.AddOwnExpenseAsync(_app, older.GroupId, ana, older.OwnerRowId);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(older.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_NewestExpenseOfTheCaller_WinsOverOlderOnesInOtherGroups()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup first = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "First");
            ExpenseGroup second = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Second");
            await DashboardSteps.AddOwnExpenseAsync(_app, first.GroupId, ana, first.OwnerRowId);
            await DashboardSteps.AddOwnExpenseAsync(_app, second.GroupId, ana, second.OwnerRowId);
            await DashboardSteps.AddOwnExpenseAsync(_app, first.GroupId, ana, first.OwnerRowId);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(first.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_AfterAnExpenseInAnotherGroup_MovesToThatGroup()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup first = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "First");
            ExpenseGroup second = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Second");
            await DashboardSteps.AddOwnExpenseAsync(_app, first.GroupId, ana, first.OwnerRowId);
            Guid? before = DashboardJson.PlusTargetGroupIdOf(await DashboardRequests.ReadAsync(ana));

            await DashboardSteps.AddOwnExpenseAsync(_app, second.GroupId, ana, second.OwnerRowId);
            Guid? after = DashboardJson.PlusTargetGroupIdOf(await DashboardRequests.ReadAsync(ana));

            Assert.Equal(first.GroupId, before);
            Assert.Equal(second.GroupId, after);
        }

        [Fact]
        public async Task PlusTarget_NewerExpenseAddedBySomeoneElse_DoesNotCount()
        {
            SignedInUser ana = await _app.SignUpAsync();
            SignedInUser bojan = await _app.SignUpAsync(GroupsApp.MemberName);
            ExpenseGroup own = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Own");
            ExpenseGroup shared = await DashboardSteps.CreateGroupAsync(_app, bojan, ana, "Shared");
            await DashboardSteps.AddOwnExpenseAsync(_app, own.GroupId, ana, own.OwnerRowId);
            await DashboardSteps.AddOwnExpenseAsync(_app, shared.GroupId, bojan, shared.OwnerRowId);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(own.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_NewerExpenseThatWasDeleted_IsSkipped()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup first = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "First");
            ExpenseGroup second = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Second");
            await DashboardSteps.AddOwnExpenseAsync(_app, first.GroupId, ana, first.OwnerRowId);
            Guid deletedId = await DashboardSteps.AddOwnExpenseAsync(_app, second.GroupId, ana, second.OwnerRowId);
            await ExpenseRequests.DeleteAsync(ana.Client, second.GroupId, deletedId);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(first.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_EveryExpenseDeleted_FallsBackToTheGroupJoinedLast()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup first = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "First");
            ExpenseGroup second = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Second");
            Guid expenseId = await DashboardSteps.AddOwnExpenseAsync(_app, first.GroupId, ana, first.OwnerRowId);
            await ExpenseRequests.DeleteAsync(ana.Client, first.GroupId, expenseId);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(second.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_NewerExpenseInAClosingGroup_IsSkipped()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup open = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Open");
            await DashboardSteps.AddOwnExpenseAsync(_app, open.GroupId, ana, open.OwnerRowId);
            await DashboardSteps.CreateClosingGroupAsync(_app, ana, "Closing");

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(open.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_OnlyGroupIsClosing_IsNull()
        {
            SignedInUser ana = await _app.SignUpAsync();
            await DashboardSteps.CreateClosingGroupAsync(_app, ana);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(JsonValueKind.Null, body.GetProperty("plusTarget").ValueKind);
        }

        [Fact]
        public async Task PlusTarget_ClosingGroupExpiredAfter24Hours_IsNotAnOpenGroup()
        {
            SignedInUser ana = await _app.SignUpAsync();
            await DashboardSteps.CreateClosingGroupAsync(_app, ana);
            _app.Clock.Advance(ClosingSteps.ClosingWindow);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(JsonValueKind.Null, body.GetProperty("plusTarget").ValueKind);
        }

        [Fact]
        public async Task PlusTarget_OnlyGroupIsFinished_IsNull()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana);
            await DashboardSteps.AddOwnExpenseAsync(_app, group.GroupId, ana, group.OwnerRowId);
            await DashboardSteps.MarkFinishedAsync(_app, group.GroupId);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(JsonValueKind.Null, body.GetProperty("plusTarget").ValueKind);
        }

        [Fact]
        public async Task PlusTarget_NoExpenseYet_JoinFallbackSkipsAClosingGroupJoinedLater()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup open = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Open");
            await DashboardSteps.CreateClosingGroupAsync(_app, ana, "Closing");

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(open.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_NewerExpenseInADeletedGroup_IsSkipped()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup kept = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Kept");
            ExpenseGroup deleted = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Deleted");
            await DashboardSteps.AddOwnExpenseAsync(_app, kept.GroupId, ana, kept.OwnerRowId);
            await DashboardSteps.AddOwnExpenseAsync(_app, deleted.GroupId, ana, deleted.OwnerRowId);
            await GroupRows.SetDeletedAsync(_app, deleted.GroupId, ana.UserId, 1);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(kept.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_NewerExpenseInAGroupTheCallerLeft_IsSkipped()
        {
            SignedInUser ana = await _app.SignUpAsync();
            SignedInUser bojan = await _app.SignUpAsync(GroupsApp.MemberName);
            ExpenseGroup stayed = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Stayed");
            ExpenseGroup left = await DashboardSteps.CreateGroupAsync(_app, bojan, ana, "Left");
            await DashboardSteps.AddOwnExpenseAsync(_app, stayed.GroupId, ana, stayed.OwnerRowId);
            await DashboardSteps.AddOwnExpenseAsync(_app, left.GroupId, ana, left.MemberRowId);
            await GroupRows.EndMembershipAsync(_app, left.GroupId, ana.UserId, "Left");

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(stayed.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_NewerExpenseInAOneBill_IsIgnoredForAnOlderNormalGroupWithAnExpense()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup normal = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Normal");
            await DashboardSteps.AddOwnExpenseAsync(_app, normal.GroupId, ana, normal.OwnerRowId);
            await DashboardSteps.CreateOneBillWithExpenseAsync(_app, ana);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(normal.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_ExpenseOnlyInAOneBill_FallsBackToTheNormalGroupJoinedLast()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup normal = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Normal");
            await DashboardSteps.CreateOneBillWithExpenseAsync(_app, ana);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(normal.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_NoExpenseYet_JoinFallbackSkipsAOneBillJoinedLater()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup normal = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Normal");
            await DashboardSteps.CreateEmptyOneBillJoinedNowAsync(_app, ana);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(normal.GroupId, DashboardJson.PlusTargetGroupIdOf(body));
        }

        [Fact]
        public async Task PlusTarget_OnlyAOneBillWithAnExpense_IsNull()
        {
            SignedInUser ana = await _app.SignUpAsync();
            await DashboardSteps.CreateOneBillWithExpenseAsync(_app, ana);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(JsonValueKind.Null, body.GetProperty("plusTarget").ValueKind);
        }

        [Fact]
        public async Task PlusTarget_OnlyAnEmptyOneBill_IsNull()
        {
            SignedInUser ana = await _app.SignUpAsync();
            await DashboardSteps.CreateEmptyOneBillJoinedNowAsync(_app, ana);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(JsonValueKind.Null, body.GetProperty("plusTarget").ValueKind);
        }

        [Fact]
        public async Task PlusTarget_OtherPeoplesGroups_NeverAppear()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana);
            await DashboardSteps.AddOwnExpenseAsync(_app, group.GroupId, ana, group.OwnerRowId);
            SignedInUser stranger = await _app.SignUpAsync();

            JsonElement body = await DashboardRequests.ReadAsync(stranger);

            Assert.Null(DashboardJson.PlusTargetGroupIdOf(body));
        }
    }
}
