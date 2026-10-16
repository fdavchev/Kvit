using System.Text.Json;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Dashboard
{
    public class DashboardTotalsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Totals_CallerIsOwedMoney_ListsItUnderOwedOnly()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await DashboardSteps.OwnerPaysForBothAsync(_app, group);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Equal([new CurrencyAmount("MKD", DashboardSteps.ShareMinor)], DashboardJson.OwedOf(body));
            Assert.Empty(DashboardJson.OweOf(body));
        }

        [Fact]
        public async Task Totals_CallerOwesMoney_ListsTheAbsoluteAmountUnderOweOnly()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await DashboardSteps.OwnerPaysForBothAsync(_app, group);

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            Assert.Equal([new CurrencyAmount("MKD", DashboardSteps.ShareMinor)], DashboardJson.OweOf(body));
            Assert.Empty(DashboardJson.OwedOf(body));
        }

        [Fact]
        public async Task Totals_GroupWithNoExpenses_ListsNothing()
        {
            ExpenseGroup group = await CreateGroupAsync();

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Empty(DashboardJson.OwedOf(body));
            Assert.Empty(DashboardJson.OweOf(body));
        }

        [Fact]
        public async Task Totals_BalancePaidOffByAConfirmedPayment_ListsNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);

            JsonElement ownerBody = await DashboardRequests.ReadAsync(group.Owner);
            JsonElement memberBody = await DashboardRequests.ReadAsync(group.Member);

            Assert.Empty(DashboardJson.OwedOf(ownerBody));
            Assert.Empty(DashboardJson.OweOf(memberBody));
        }

        [Fact]
        public async Task Totals_PendingPayment_CountsExactlyLikeTheBalancesTab()
        {
            const long pendingMinor = 10_000;
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, pendingMinor);
            long anaOnTheBalancesTab = await DashboardSteps.BalanceOfAsync(group.Owner, group.GroupId, group.OwnerRowId, "MKD");
            long bojanOnTheBalancesTab = await DashboardSteps.BalanceOfAsync(group.Member, group.GroupId, group.MemberRowId, "MKD");

            JsonElement ownerBody = await DashboardRequests.ReadAsync(group.Owner);
            JsonElement memberBody = await DashboardRequests.ReadAsync(group.Member);

            Assert.Equal(DashboardSteps.ShareMinor, anaOnTheBalancesTab);
            Assert.Equal([new CurrencyAmount("MKD", anaOnTheBalancesTab)], DashboardJson.OwedOf(ownerBody));
            Assert.Equal([new CurrencyAmount("MKD", -bojanOnTheBalancesTab)], DashboardJson.OweOf(memberBody));
        }

        [Fact]
        public async Task Totals_SameCurrencyInTwoGroups_SumsOwedAndOweSeparatelyWithoutNetting()
        {
            SignedInUser ana = await _app.SignUpAsync();
            SignedInUser bojan = await _app.SignUpAsync(GroupsApp.MemberName);
            ExpenseGroup anaPays = await DashboardSteps.CreateGroupAsync(_app, ana, bojan);
            ExpenseGroup bojanPays = await DashboardSteps.CreateGroupAsync(_app, bojan, ana);
            await DashboardSteps.OwnerPaysForBothAsync(_app, anaPays);
            await DashboardSteps.OwnerPaysForBothAsync(_app, anaPays);
            await DashboardSteps.OwnerPaysForBothAsync(_app, bojanPays);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal([new CurrencyAmount("MKD", 2 * DashboardSteps.ShareMinor)], DashboardJson.OwedOf(body));
            Assert.Equal([new CurrencyAmount("MKD", DashboardSteps.ShareMinor)], DashboardJson.OweOf(body));
        }

        [Fact]
        public async Task Totals_TwoGroupsOwedInTheSameCurrency_AddsTheirBalances()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup first = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana);
            ExpenseGroup second = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana);
            await DashboardSteps.OwnerPaysForBothAsync(_app, first);
            await DashboardSteps.OwnerPaysForBothAsync(_app, second);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal([new CurrencyAmount("MKD", 2 * DashboardSteps.ShareMinor)], DashboardJson.OwedOf(body));
        }

        [Fact]
        public async Task Totals_BothCurrencies_ListsMkdBeforeEurAndNeverMixesThem()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup eurGroup = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana);
            ExpenseGroup mkdGroup = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana);
            await DashboardSteps.OwnerPaysForBothAsync(_app, eurGroup, ExpenseInputs.Eur);
            await DashboardSteps.OwnerPaysForBothAsync(_app, mkdGroup);
            await DashboardSteps.OwnerPaysForBothAsync(_app, mkdGroup);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(
                [new CurrencyAmount("MKD", 2 * DashboardSteps.ShareMinor), new CurrencyAmount("EUR", DashboardSteps.ShareMinor)],
                DashboardJson.OwedOf(body));
            Assert.Empty(DashboardJson.OweOf(body));
        }

        [Fact]
        public async Task Totals_OwedInMkdAndOwingInEur_KeepsEachInItsOwnList()
        {
            SignedInUser ana = await _app.SignUpAsync();
            SignedInUser bojan = await _app.SignUpAsync(GroupsApp.MemberName);
            ExpenseGroup mkdGroup = await DashboardSteps.CreateGroupAsync(_app, ana, bojan);
            ExpenseGroup eurGroup = await DashboardSteps.CreateGroupAsync(_app, bojan, ana);
            await DashboardSteps.OwnerPaysForBothAsync(_app, mkdGroup);
            await DashboardSteps.OwnerPaysForBothAsync(_app, eurGroup, ExpenseInputs.Eur);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal([new CurrencyAmount("MKD", DashboardSteps.ShareMinor)], DashboardJson.OwedOf(body));
            Assert.Equal([new CurrencyAmount("EUR", DashboardSteps.ShareMinor)], DashboardJson.OweOf(body));
        }

        [Fact]
        public async Task Totals_OnlyEurInUse_ListsNoMkdEntry()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await DashboardSteps.OwnerPaysForBothAsync(_app, group, ExpenseInputs.Eur);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Equal([new CurrencyAmount("EUR", DashboardSteps.ShareMinor)], DashboardJson.OwedOf(body));
        }

        [Fact]
        public async Task Totals_MemberWhoseOwnBalanceIsZero_HasNoEntry()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);

            JsonElement body = await DashboardRequests.ReadAsync(group.Third);

            Assert.Empty(DashboardJson.OwedOf(body));
            Assert.Empty(DashboardJson.OweOf(body));
        }

        [Fact]
        public async Task Totals_ExpenseDeletedAfterwards_NoLongerCounts()
        {
            ExpenseGroup group = await CreateGroupAsync();
            Guid expenseId = await DashboardSteps.OwnerPaysForBothAsync(_app, group);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Empty(DashboardJson.OwedOf(body));
        }

        [Fact]
        public async Task Totals_FinishedGroupWithMoneyInIt_IsNotCounted()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await DashboardSteps.OwnerPaysForBothAsync(_app, group);
            await DashboardSteps.MarkFinishedAsync(_app, group.GroupId);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Empty(DashboardJson.OwedOf(body));
        }

        [Fact]
        public async Task Totals_ClosingGroupWithMoneyInIt_IsCounted()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await DashboardSteps.OwnerPaysForBothAsync(_app, group);
            await DashboardSteps.MarkClosingAsync(_app, group.GroupId);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Equal([new CurrencyAmount("MKD", DashboardSteps.ShareMinor)], DashboardJson.OwedOf(body));
        }

        [Fact]
        public async Task Totals_ClosingGroupOlderThan24Hours_IsNotCountedBecauseItIsEffectivelyFinished()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await DashboardSteps.OwnerPaysForBothAsync(_app, group);
            await DashboardSteps.MarkClosingAsync(_app, group.GroupId);
            _app.Clock.Advance(ClosingSteps.ClosingWindow);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Empty(DashboardJson.OwedOf(body));
        }

        [Fact]
        public async Task Totals_DeletedGroup_IsNotCounted()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await DashboardSteps.OwnerPaysForBothAsync(_app, group);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 1);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Empty(DashboardJson.OwedOf(body));
        }

        [Fact]
        public async Task Totals_GroupTheCallerNoLongerBelongsTo_IsNotCounted()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await DashboardSteps.OwnerPaysForBothAsync(_app, group);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Removed");

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            Assert.Empty(DashboardJson.OweOf(body));
        }

        [Fact]
        public async Task Totals_MoneyOfOtherPeoplesGroups_NeverShowsUp()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await DashboardSteps.OwnerPaysForBothAsync(_app, group);
            SignedInUser stranger = await _app.SignUpAsync();

            JsonElement body = await DashboardRequests.ReadAsync(stranger);

            Assert.Empty(DashboardJson.OwedOf(body));
            Assert.Empty(DashboardJson.OweOf(body));
        }

        private async Task<ExpenseGroup> CreateGroupAsync()
        {
            SignedInUser owner = await _app.SignUpAsync();

            return await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner);
        }
    }
}
