using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public class ClosingExpiryReadTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int JustInsideTheWindowSeconds = (24 * 60 * 60) - 1;
        private const int ExactlyTheWindowSeconds = 24 * 60 * 60;

        [Fact]
        public async Task GetGroup_ClosingForLessThan24Hours_ShowsClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(JustInsideTheWindowSeconds));

            Assert.Equal("Closing", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
            Assert.Equal("Closing", await ClosingSteps.StatusInGroupResponseAsync(group.Member, group.GroupId));
        }

        [Fact]
        public async Task GetGroup_ClosingFor24Hours_ShowsFinishedToEveryoneButTheStoredRowStaysClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            string startedAt = (await ClosingRows.ClosingStartedAtUtcAsync(_app, group.GroupId))!;
            _app.Clock.Advance(TimeSpan.FromSeconds(ExactlyTheWindowSeconds));

            Assert.Equal("Finished", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
            Assert.Equal("Finished", await ClosingSteps.StatusInGroupResponseAsync(group.Member, group.GroupId));
            Assert.Equal("Finished", await ClosingSteps.StatusInGroupResponseAsync(group.Third, group.GroupId));
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(startedAt, await ClosingRows.ClosingStartedAtUtcAsync(_app, group.GroupId));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "finished_at"));
        }

        [Fact]
        public async Task GetGroup_FinishedByTheConfirmations_ShowsFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            Assert.Equal("Finished", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task GetGroup_AfterAnObjection_ShowsOpen()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ObjectAsync(group.Member, group.GroupId);

            Assert.Equal("Open", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task GetList_ClosingForLessThan24Hours_StaysInGroups()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(JustInsideTheWindowSeconds));

            JsonElement body = await ClosingSteps.ReadGroupListAsync(group.Member);

            Assert.Contains(group.GroupId, GroupRequests.IdsOf(body.GetProperty("groups")));
            Assert.DoesNotContain(group.GroupId, GroupRequests.IdsOf(body.GetProperty("finishedGroups")));
        }

        [Fact]
        public async Task GetList_ClosingFor24Hours_IsInFinishedGroupsAndNotInGroupsButTheStoredRowStaysClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(ExactlyTheWindowSeconds));

            JsonElement ownerBody = await ClosingSteps.ReadGroupListAsync(group.Owner);
            JsonElement memberBody = await ClosingSteps.ReadGroupListAsync(group.Member);

            Assert.Contains(group.GroupId, GroupRequests.IdsOf(ownerBody.GetProperty("finishedGroups")));
            Assert.DoesNotContain(group.GroupId, GroupRequests.IdsOf(ownerBody.GetProperty("groups")));
            Assert.Contains(group.GroupId, GroupRequests.IdsOf(memberBody.GetProperty("finishedGroups")));
            Assert.DoesNotContain(group.GroupId, GroupRequests.IdsOf(memberBody.GetProperty("groups")));
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task GetList_FinishedByTheConfirmations_IsInFinishedGroups()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            JsonElement body = await ClosingSteps.ReadGroupListAsync(group.Third);

            Assert.Contains(group.GroupId, GroupRequests.IdsOf(body.GetProperty("finishedGroups")));
            Assert.DoesNotContain(group.GroupId, GroupRequests.IdsOf(body.GetProperty("groups")));
        }

        [Fact]
        public async Task GetList_AfterAnUnlock_PutsTheGroupBackInGroups()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);

            JsonElement body = await ClosingSteps.ReadGroupListAsync(group.Member);

            Assert.Contains(group.GroupId, GroupRequests.IdsOf(body.GetProperty("groups")));
            Assert.DoesNotContain(group.GroupId, GroupRequests.IdsOf(body.GetProperty("finishedGroups")));
        }

        [Fact]
        public async Task Get_GroupAndListOfAnExpiredClosing_WriteNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            _app.Clock.Advance(TimeSpan.FromSeconds(ExactlyTheWindowSeconds));
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);
            int responsesBefore = await ClosingResponseRows.CountAsync(_app, group.GroupId);

            await ClosingSteps.ReadGroupAsync(group.Owner, group.GroupId);
            await ClosingSteps.ReadGroupAsync(group.Member, group.GroupId);
            await ClosingSteps.ReadGroupListAsync(group.Owner);
            await ClosingSteps.ReadGroupListAsync(group.Third);

            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(responsesBefore, await ClosingResponseRows.CountAsync(_app, group.GroupId));
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
        }
    }
}
