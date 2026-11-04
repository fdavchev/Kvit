using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportReadOnlyTests(ExportApp _app) : IClassFixture<ExportApp>
    {
        [Fact]
        public async Task Export_RichAccount_ChangesNoRowInAnyTable()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            string before = await ExportRows.SnapshotOfEveryTableAsync(_app);

            await ExportRequests.GetBodyAsync(scenario.Exporter.Client);

            Assert.Equal(before, await ExportRows.SnapshotOfEveryTableAsync(_app));
        }

        [Fact]
        public async Task Export_RichAccount_WritesNoUsageEvent()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            int before = await ExportRows.CountUsageEventsAsync(_app);

            await ExportRequests.GetBodyAsync(scenario.Exporter.Client);

            Assert.Equal(before, await ExportRows.CountUsageEventsAsync(_app));
        }

        [Fact]
        public async Task Export_Guest_ChangesNoRowInAnyTable()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            string before = await ExportRows.SnapshotOfEveryTableAsync(_app);

            await ExportRequests.GetBodyAsync(guest.Client);

            Assert.Equal(before, await ExportRows.SnapshotOfEveryTableAsync(_app));
        }

        [Fact]
        public async Task Export_AccountWithNothing_ChangesNoRowInAnyTable()
        {
            SignedInUser user = await _app.SignUpAsync();
            string before = await ExportRows.SnapshotOfEveryTableAsync(_app);

            await ExportRequests.GetBodyAsync(user.Client);

            Assert.Equal(before, await ExportRows.SnapshotOfEveryTableAsync(_app));
        }

        [Fact]
        public async Task Export_CalledTwiceAtTheSameMoment_AnswersTheSameContent()
        {
            ExportScenario scenario = await _app.ScenarioAsync();

            ExportBody first = await ExportRequests.GetBodyAsync(scenario.Exporter.Client);
            ExportBody second = await ExportRequests.GetBodyAsync(scenario.Exporter.Client);

            Assert.Equal(first.Raw, second.Raw);
        }

        [Fact]
        public async Task Export_GroupWhoseClosingRanOut_ShowsFinishedAndLeavesTheStoredStatusAlone()
        {
            _app.Clock.SetTo(ExpensesApp.ClockStart);
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);
            await ExportRows.StartClosingAsync(_app, groupId, _app.Clock.GetUtcNow() - TimeSpan.FromHours(25));

            ExportBody body = await ExportRequests.GetBodyAsync(owner.Client);

            Assert.Equal("Finished", body.GroupWithId(groupId).GetProperty("status").GetString());
            Assert.Equal("Closing", await GroupRows.GroupValueAsync(_app, groupId, "status"));
        }

        [Fact]
        public async Task Export_GroupBeingClosedForLessThan24Hours_ShowsClosing()
        {
            _app.Clock.SetTo(ExpensesApp.ClockStart);
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);
            await ExportRows.StartClosingAsync(_app, groupId, _app.Clock.GetUtcNow() - TimeSpan.FromHours(23));

            ExportBody body = await ExportRequests.GetBodyAsync(owner.Client);

            Assert.Equal("Closing", body.GroupWithId(groupId).GetProperty("status").GetString());
        }
    }
}
