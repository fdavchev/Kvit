using System.Text.Json;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Dashboard
{
    public class DashboardNeedsYouClosingTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task NeedsYou_ClosingGroupWithNoAnswerYet_AnswersAClosingItemWithExactlyTheFourFields()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            JsonElement item = Assert.Single(DashboardJson.ClosingItemsOf(body));
            Assert.Equal(DashboardJson.ClosingItemFieldNames, GroupRequests.PropertyNamesOf(item));
        }

        [Fact]
        public async Task NeedsYou_ClosingGroupWithNoAnswerYet_AnswersTheGroupAndWhoStartedIt()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            JsonElement item = Assert.Single(DashboardJson.ClosingItemsOf(body));
            Assert.Equal("closing", item.GetProperty("type").GetString());
            Assert.Equal(group.GroupId, item.GetProperty("groupId").GetGuid());
            Assert.Equal(GroupRows.DefaultName, item.GetProperty("groupName").GetString());
            Assert.Equal(GroupsApp.OwnerName, item.GetProperty("startedByName").GetString());
        }

        [Fact]
        public async Task NeedsYou_ClosingGroup_ShowsToEveryMemberWithAnAccountExceptTheOwner()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            JsonElement ownerBody = await DashboardRequests.ReadAsync(group.Owner);
            JsonElement memberBody = await DashboardRequests.ReadAsync(group.Member);
            JsonElement thirdBody = await DashboardRequests.ReadAsync(group.Third);

            Assert.Empty(DashboardJson.ClosingItemsOf(ownerBody));
            Assert.Equal([group.GroupId], DashboardJson.ClosingGroupIdsOf(memberBody));
            Assert.Equal([group.GroupId], DashboardJson.ClosingGroupIdsOf(thirdBody));
        }

        [Fact]
        public async Task NeedsYou_MemberWhoConfirmed_NoLongerSeesItWhileTheOtherMemberStillDoes()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);

            JsonElement memberBody = await DashboardRequests.ReadAsync(group.Member);
            JsonElement thirdBody = await DashboardRequests.ReadAsync(group.Third);

            Assert.Empty(DashboardJson.ClosingItemsOf(memberBody));
            Assert.Equal([group.GroupId], DashboardJson.ClosingGroupIdsOf(thirdBody));
        }

        [Fact]
        public async Task NeedsYou_AfterAnObjection_NobodyHasAClosingItemBecauseTheGroupIsOpenAgain()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ObjectAsync(group.Member, group.GroupId);

            JsonElement memberBody = await DashboardRequests.ReadAsync(group.Member);
            JsonElement thirdBody = await DashboardRequests.ReadAsync(group.Third);

            Assert.Empty(DashboardJson.ClosingItemsOf(memberBody));
            Assert.Empty(DashboardJson.ClosingItemsOf(thirdBody));
        }

        [Fact]
        public async Task NeedsYou_ClosingGroupExpiredAfter24Hours_ShowsNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(ClosingSteps.ClosingWindow);

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            Assert.Empty(DashboardJson.ClosingItemsOf(body));
        }

        [Fact]
        public async Task NeedsYou_ClosingGroupJustInsideTheWindow_StillShows()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(ClosingSteps.ClosingWindow - TimeSpan.FromSeconds(1));

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            Assert.Equal([group.GroupId], DashboardJson.ClosingGroupIdsOf(body));
        }

        [Fact]
        public async Task NeedsYou_FinishedGroup_ShowsNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            JsonElement body = await DashboardRequests.ReadAsync(group.Third);

            Assert.Empty(DashboardJson.ClosingItemsOf(body));
        }

        [Fact]
        public async Task NeedsYou_DeletedClosingGroup_ShowsNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 1);

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            Assert.Empty(DashboardJson.ClosingItemsOf(body));
        }

        [Fact]
        public async Task NeedsYou_ClosingGroupTheCallerNoLongerBelongsTo_ShowsNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Removed");

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            Assert.Empty(DashboardJson.ClosingItemsOf(body));
        }

        [Fact]
        public async Task NeedsYou_TwoClosingGroups_ListsBothWithTheirOwnName()
        {
            SignedInUser bojan = await _app.SignUpAsync(GroupsApp.MemberName);
            SignedInUser flatOwner = await _app.SignUpAsync();
            SignedInUser tripOwner = await _app.SignUpAsync();
            ExpenseGroup flat = await DashboardSteps.CreateGroupAsync(_app, flatOwner, bojan, "Flat");
            ExpenseGroup trip = await DashboardSteps.CreateGroupAsync(_app, tripOwner, bojan, "Trip");
            await ClosingSteps.AddCountedExpenseAsync(_app, flat.GroupId, flatOwner, flat.OwnerRowId);
            await ClosingSteps.AddCountedExpenseAsync(_app, trip.GroupId, tripOwner, trip.OwnerRowId);
            await ClosingSteps.StartAsync(flatOwner, flat.GroupId);
            await ClosingSteps.StartAsync(tripOwner, trip.GroupId);

            JsonElement body = await DashboardRequests.ReadAsync(bojan);

            List<JsonElement> items = DashboardJson.ClosingItemsOf(body);
            Assert.Equal(2, items.Count);
            Assert.Equal("Flat", items.Single(item => item.GetProperty("groupId").GetGuid() == flat.GroupId).GetProperty("groupName").GetString());
            Assert.Equal("Trip", items.Single(item => item.GetProperty("groupId").GetGuid() == trip.GroupId).GetProperty("groupName").GetString());
        }

        [Fact]
        public async Task NeedsYou_ClosingItemAndPaymentItem_AreBothListedInTheSameAnswer()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            ExpenseGroup other = await DashboardSteps.CreateGroupAsync(_app, group.Third, group.Member, "Other");
            Guid settlementId = await SettlementSteps.RecordAsync(group.Third, other.GroupId, other.OwnerRowId, other.MemberRowId);

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            Assert.Equal([group.GroupId], DashboardJson.ClosingGroupIdsOf(body));
            Assert.Equal([settlementId], DashboardJson.SettlementIdsOf(body));
            Assert.Equal(2, body.GetProperty("needsYou").GetArrayLength());
        }
    }
}
