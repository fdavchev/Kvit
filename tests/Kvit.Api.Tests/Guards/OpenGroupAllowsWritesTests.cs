using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public class OpenGroupAllowsWritesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [MemberData(nameof(GuardedWrites.NamesCalledByMembers), MemberType = typeof(GuardedWrites))]
        public async Task Write_ByAMemberInAnOpenGroup_Succeeds(string name)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SendWrite send = await GuardedWrites.Named(name).PrepareAsync(_app, group);

            HttpResponseMessage response = await send(group.Member.Client);

            await GuardSteps.AssertSucceededAsync(response);
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(GuardedWrites.NamesCalledByTheOwner), MemberType = typeof(GuardedWrites))]
        public async Task Write_ByTheOwnerInAnOpenGroup_Succeeds(string name)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SendWrite send = await GuardedWrites.Named(name).PrepareAsync(_app, group);

            HttpResponseMessage response = await send(group.Owner.Client);

            await GuardSteps.AssertSucceededAsync(response);
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
        }
    }
}
