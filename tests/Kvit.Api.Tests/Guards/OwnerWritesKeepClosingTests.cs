using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public class OwnerWritesKeepClosingTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [MemberData(nameof(GuardedWrites.NamesThatLeaveAClosingAlone), MemberType = typeof(GuardedWrites))]
        public async Task Write_ByTheOwnerWhileTheGroupIsClosing_SucceedsAndLeavesTheClosingExactlyAsItWas(string name)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named(name);
            SendWrite send = await write.PrepareAsync(_app, group);
            await GuardStates.CloseAsync(_app, group, write);
            ClosingFacts before = await ClosingFacts.ReadAsync(_app, group.GroupId);

            HttpResponseMessage response = await send(group.Owner.Client);

            await GuardSteps.AssertSucceededAsync(response);
            Assert.Equal("Closing", before.Status);
            Assert.Equal(1, before.ResponseCount);
            Assert.Equal(before, await ClosingFacts.ReadAsync(_app, group.GroupId));
        }
    }
}
