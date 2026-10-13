using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class SettlementsReadOnlyTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task List_ChangesNothingInTheSettlementsTheEventsOrTheUsageLog()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            await SettlementSteps.PendingBojanToDimeAsync(group);
            Guid rejected = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, rejected);
            string settlementsBefore = await SettlementRows.SnapshotAsync(_app, group.GroupId);
            string groupBefore = await GroupRows.SnapshotAsync(_app, group.GroupId);

            await SettlementSteps.ReadListAsync(group.Owner, group.GroupId);
            await SettlementSteps.ReadListAsync(group.Member, group.GroupId);
            await SettlementSteps.ReadListAsync(group.Third, group.GroupId);

            Assert.Equal(settlementsBefore, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(groupBefore, await GroupRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task List_KeepsTheRowCountsOfSettlementsMembersAndEvents()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            await SettlementSteps.PendingBojanToDimeAsync(group);
            int settlementsBefore = await SettlementRows.CountAsync(_app, group.GroupId);
            int membersBefore = await GroupRows.CountMembersAsync(_app, group.GroupId);
            int eventsBefore = await GroupRows.CountEventsAsync(_app, group.GroupId);

            await SettlementSteps.ReadListAsync(group.Owner, group.GroupId);

            Assert.Equal(2, settlementsBefore);
            Assert.Equal(5, membersBefore);
            Assert.Equal(settlementsBefore, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal(membersBefore, await GroupRows.CountMembersAsync(_app, group.GroupId));
            Assert.Equal(eventsBefore, await GroupRows.CountEventsAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Balances_AfterSettlementsChangesNothingInTheSettlementRows()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            await SettlementSteps.PendingBojanToDimeAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD");

            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }
    }
}
