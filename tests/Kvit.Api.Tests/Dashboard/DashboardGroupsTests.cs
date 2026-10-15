using System.Text.Json;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Dashboard
{
    public class DashboardGroupsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Groups_Row_AnswersExactlyTheSixFields()
        {
            SignedInUser owner = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner);

            JsonElement body = await DashboardRequests.ReadAsync(owner);

            JsonElement row = DashboardJson.GroupRowOf(body, group.GroupId);
            Assert.Equal(DashboardJson.GroupFieldNames, GroupRequests.PropertyNamesOf(row));
        }

        [Fact]
        public async Task Groups_Row_AnswersTheKindNameEmojiAndOpenStatus()
        {
            SignedInUser owner = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner, "Flat");

            JsonElement body = await DashboardRequests.ReadAsync(owner);

            JsonElement row = DashboardJson.GroupRowOf(body, group.GroupId);
            Assert.Equal("Group", row.GetProperty("kind").GetString());
            Assert.Equal("Flat", row.GetProperty("name").GetString());
            Assert.Equal(GroupRows.DefaultEmoji, row.GetProperty("emoji").GetString());
            Assert.Equal("Open", row.GetProperty("status").GetString());
        }

        [Fact]
        public async Task Groups_OneBill_AnswersKindOneBill()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await ClosingSteps.CreateEmptyOneBillAsync(_app, owner);

            JsonElement body = await DashboardRequests.ReadAsync(owner);

            Assert.Equal("OneBill", DashboardJson.GroupRowOf(body, groupId).GetProperty("kind").GetString());
        }

        [Fact]
        public async Task Groups_NewGroupWithNoExpenses_HasNoBalances()
        {
            SignedInUser owner = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner);

            JsonElement body = await DashboardRequests.ReadAsync(owner);

            Assert.Empty(DashboardJson.BalancesOfGroup(body, group.GroupId));
        }

        [Fact]
        public async Task Groups_Balances_AnswerTheCallersOwnSignedBalance()
        {
            SignedInUser owner = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner);
            await DashboardSteps.OwnerPaysForBothAsync(_app, group);

            JsonElement ownerBody = await DashboardRequests.ReadAsync(owner);
            JsonElement memberBody = await DashboardRequests.ReadAsync(group.Member);

            Assert.Equal([new CurrencyAmount("MKD", DashboardSteps.ShareMinor)], DashboardJson.BalancesOfGroup(ownerBody, group.GroupId));
            Assert.Equal([new CurrencyAmount("MKD", -DashboardSteps.ShareMinor)], DashboardJson.BalancesOfGroup(memberBody, group.GroupId));
        }

        [Fact]
        public async Task Groups_Balances_SkipACurrencyWhereTheCallerIsEven()
        {
            SignedInUser owner = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner);
            await DashboardSteps.MemberPaysForBothAsync(_app, group, ExpenseInputs.Eur);
            await DashboardSteps.OwnerPaysForBothAsync(_app, group);
            await DashboardSteps.MemberPaysForBothAsync(_app, group);

            JsonElement body = await DashboardRequests.ReadAsync(owner);

            Assert.Equal([new CurrencyAmount("EUR", -DashboardSteps.ShareMinor)], DashboardJson.BalancesOfGroup(body, group.GroupId));
        }

        [Fact]
        public async Task Groups_Balances_WithBothCurrenciesOpen_ListMkdBeforeEur()
        {
            SignedInUser owner = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner);
            await DashboardSteps.MemberPaysForBothAsync(_app, group, ExpenseInputs.Eur);
            await DashboardSteps.OwnerPaysForBothAsync(_app, group);

            JsonElement body = await DashboardRequests.ReadAsync(owner);

            Assert.Equal(
                [new CurrencyAmount("MKD", DashboardSteps.ShareMinor), new CurrencyAmount("EUR", -DashboardSteps.ShareMinor)],
                DashboardJson.BalancesOfGroup(body, group.GroupId));
        }

        [Fact]
        public async Task Groups_Balances_MatchTheBalancesTabOfEachGroup()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 10_000);
            long onTheBalancesTab = await DashboardSteps.BalanceOfAsync(group.Owner, group.GroupId, group.OwnerRowId, "MKD");

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Equal([new CurrencyAmount("MKD", onTheBalancesTab)], DashboardJson.BalancesOfGroup(body, group.GroupId));
        }

        [Fact]
        public async Task Groups_PaidUpGroup_HasNoBalances()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Empty(DashboardJson.BalancesOfGroup(body, group.GroupId));
        }

        [Fact]
        public async Task Groups_TwoGroups_AreNewestCreatedFirst()
        {
            SignedInUser owner = await _app.SignUpAsync();
            ExpenseGroup oldest = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner, "Oldest");
            ExpenseGroup middle = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner, "Middle");
            ExpenseGroup newest = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner, "Newest");
            await DashboardSteps.AddOwnExpenseAsync(_app, oldest.GroupId, owner, oldest.OwnerRowId);

            JsonElement body = await DashboardRequests.ReadAsync(owner);

            Assert.Equal([newest.GroupId, middle.GroupId, oldest.GroupId], DashboardJson.GroupIdsOf(body));
        }

        [Fact]
        public async Task Groups_ListsTheSameGroupsAsTheGroupsListInTheSameOrder()
        {
            SignedInUser owner = await _app.SignUpAsync();
            await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner, "First");
            await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner, "Second");

            JsonElement dashboard = await DashboardRequests.ReadAsync(owner);
            JsonElement list = await ClosingSteps.ReadGroupListAsync(owner);

            Assert.Equal(GroupRequests.IdsOf(list.GetProperty("groups")), DashboardJson.GroupIdsOf(dashboard));
        }

        [Fact]
        public async Task Groups_GroupWhereTheCallerIsOnlyAMember_IsListed()
        {
            SignedInUser member = await _app.SignUpAsync();
            SignedInUser owner = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupAsync(_app, owner, member);

            JsonElement body = await DashboardRequests.ReadAsync(member);

            Assert.Equal([group.GroupId], DashboardJson.GroupIdsOf(body));
        }

        [Fact]
        public async Task Groups_ClosingGroup_AnswersStatusClosingToTheOwnerAndTheMembers()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            JsonElement ownerBody = await DashboardRequests.ReadAsync(group.Owner);
            JsonElement memberBody = await DashboardRequests.ReadAsync(group.Member);

            Assert.Equal("Closing", DashboardJson.GroupRowOf(ownerBody, group.GroupId).GetProperty("status").GetString());
            Assert.Equal("Closing", DashboardJson.GroupRowOf(memberBody, group.GroupId).GetProperty("status").GetString());
        }

        [Fact]
        public async Task Groups_ClosingGroupExpiredAfter24Hours_IsNotListedAtAll()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(ClosingSteps.ClosingWindow);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.DoesNotContain(group.GroupId, DashboardJson.GroupIdsOf(body));
        }

        [Fact]
        public async Task Groups_ClosingGroupJustInsideTheWindow_StillAnswersClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(ClosingSteps.ClosingWindow - TimeSpan.FromSeconds(1));

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            Assert.Equal("Closing", DashboardJson.GroupRowOf(body, group.GroupId).GetProperty("status").GetString());
        }

        [Fact]
        public async Task Groups_FinishedGroup_IsNotListed()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.DoesNotContain(group.GroupId, DashboardJson.GroupIdsOf(body));
        }

        [Fact]
        public async Task Groups_AfterAnObjection_ListsTheGroupAsOpen()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ObjectAsync(group.Member, group.GroupId);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Equal("Open", DashboardJson.GroupRowOf(body, group.GroupId).GetProperty("status").GetString());
        }

        [Fact]
        public async Task Groups_AfterAnUnlock_ListsTheGroupAsOpenAgain()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            Assert.Equal("Open", DashboardJson.GroupRowOf(body, group.GroupId).GetProperty("status").GetString());
        }

        [Fact]
        public async Task Groups_DeletedGroup_IsNotListed()
        {
            SignedInUser owner = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, owner.UserId, 1);

            JsonElement body = await DashboardRequests.ReadAsync(owner);

            Assert.Empty(DashboardJson.GroupIdsOf(body));
        }

        [Theory]
        [InlineData("Removed")]
        [InlineData("Left")]
        [InlineData("SetAside")]
        public async Task Groups_GroupWhoseMembershipEnded_IsNotListed(string endKind)
        {
            SignedInUser owner = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            Assert.Empty(DashboardJson.GroupIdsOf(body));
        }

        [Fact]
        public async Task Groups_OtherPeoplesGroups_NeverAppear()
        {
            SignedInUser owner = await _app.SignUpAsync();
            ExpenseGroup others = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner);
            SignedInUser stranger = await _app.SignUpAsync();
            ExpenseGroup own = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, stranger);

            JsonElement body = await DashboardRequests.ReadAsync(stranger);

            Assert.Equal([own.GroupId], DashboardJson.GroupIdsOf(body));
            Assert.DoesNotContain(others.GroupId, DashboardJson.GroupIdsOf(body));
        }
    }
}
