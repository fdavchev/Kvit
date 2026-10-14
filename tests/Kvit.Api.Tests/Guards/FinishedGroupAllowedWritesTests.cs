using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public class FinishedGroupAllowedWritesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData("Leave")]
        [InlineData("DeleteGroup")]
        [InlineData("RestoreGroup")]
        public async Task Write_InAFinishedGroup_StillSucceedsAndTheGroupStaysFinished(string name)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named(name);
            SendWrite send = await write.PrepareAsync(_app, group);
            await GuardStates.ForceFinishedAsync(_app, group.GroupId);
            ClosingFacts before = await ClosingFacts.ReadAsync(_app, group.GroupId);
            HttpClient caller = write.Who == WhoMayCall.MemberOnly ? group.Member.Client : group.Owner.Client;

            HttpResponseMessage response = await send(caller);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal(before, await ClosingFacts.ReadAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Leave_ByAMemberOfAGroupThatFinishedByConfirmations_EndsTheirRowAsLeft()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Left", await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "end_kind"));
            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "MemberLeft"));
        }

        [Fact]
        public async Task DeleteGroup_ByTheOwnerOfAGroupThatFinishedByConfirmations_MarksItDeletedAndKeepsItFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.NotNull(await GroupRows.GroupValueAsync(_app, group.GroupId, "deleted_at"));
            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task RestoreGroup_ByTheOwnerOfADeletedFinishedGroup_BringsItBackStillFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            await SettlementSteps.AssertNoContentAsync(await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId));

            HttpResponseMessage response = await GroupRequests.RestoreAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "deleted_at"));
            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
        }
    }
}
