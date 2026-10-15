using System.Net;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Dashboard
{
    public class DashboardReadOnlyTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_ExpiredClosingGroup_WritesNothingAndKeepsTheStoredStatusClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            _app.Clock.Advance(ClosingSteps.ClosingWindow);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);
            int responsesBefore = await ClosingResponseRows.CountAsync(_app, group.GroupId);

            await DashboardRequests.ReadAsync(group.Owner);
            await DashboardRequests.ReadAsync(group.Member);
            await DashboardRequests.ReadAsync(group.Third);

            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(responsesBefore, await ClosingResponseRows.CountAsync(_app, group.GroupId));
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "finished_at"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
        }

        [Fact]
        public async Task Get_ClosingGroupWithPendingAnswers_WritesNoClosingResponse()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            await DashboardRequests.ReadAsync(group.Member);
            await DashboardRequests.ReadAsync(group.Third);

            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, group.GroupId));
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Get_AnyUser_WritesNoUsageEventNotEvenActive()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            Guid[] userIds = [group.Owner.UserId, group.Member.UserId, group.Third.UserId];
            int before = await ClosingRows.CountUsageEventsOfAsync(_app, userIds);

            await DashboardRequests.ReadAsync(group.Owner);
            await DashboardRequests.ReadAsync(group.Member);
            await DashboardRequests.ReadAsync(group.Third);

            Assert.Equal(before, await ClosingRows.CountUsageEventsOfAsync(_app, userIds));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "Active"));
        }

        [Fact]
        public async Task Get_GroupWithMoneyAndAPendingPayment_WritesNothingToTheGroupTables()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await SettlementSteps.PendingBojanToDimeAsync(group);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            await DashboardRequests.ReadAsync(group.Owner);
            await DashboardRequests.ReadAsync(group.Third);

            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Get_Twice_AnswersTheSameBodyBothTimes()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await SettlementSteps.PendingBojanToDimeAsync(group);

            HttpResponseMessage first = await DashboardRequests.GetAsync(group.Third.Client);
            HttpResponseMessage second = await DashboardRequests.GetAsync(group.Third.Client);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Equal(
                await first.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
                await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
    }
}
