using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public class OwnerWritesCancelClosingTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [MemberData(nameof(GuardedWrites.NamesThatCancelAClosing), MemberType = typeof(GuardedWrites))]
        public async Task Write_ByTheOwnerWhileTheGroupIsClosing_SucceedsAndTurnsTheGroupBackToOpen(string name)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named(name);
            SendWrite send = await write.PrepareAsync(_app, group);
            await GuardStates.CloseAsync(_app, group, write);

            HttpResponseMessage response = await send(group.Owner.Client);

            await GuardSteps.AssertSucceededAsync(response);
            ClosingFacts after = await ClosingFacts.ReadAsync(_app, group.GroupId);
            Assert.Equal("Open", after.Status);
            Assert.Null(after.StartedAtUtc);
            Assert.Null(after.FinishedAtUtc);
            Assert.Equal(0, after.ResponseCount);
        }

        [Theory]
        [MemberData(nameof(GuardedWrites.NamesThatCancelAClosing), MemberType = typeof(GuardedWrites))]
        public async Task Write_ByTheOwnerWhileTheGroupIsClosing_WritesOneClosingCancelledEventByTheOwner(string name)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named(name);
            SendWrite send = await write.PrepareAsync(_app, group);
            await GuardStates.CloseAsync(_app, group, write);

            await GuardSteps.AssertSucceededAsync(await send(group.Owner.Client));

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingCancelled"));
            Assert.Equal(group.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingCancelled", "actor_user_id"));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingCancelled", "created_at AT TIME ZONE 'UTC'"));
        }

        [Fact]
        public async Task Write_ByTheOwnerAfterAMemberConfirmed_DeletesTheConfirmationSoTheNextClosingStartsFromNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named("AddExpense");
            SendWrite send = await write.PrepareAsync(_app, group);
            await GuardStates.CloseAsync(_app, group, write);
            Assert.Equal(1, await ClosingResponseRows.CountAsync(_app, group.GroupId));

            await GuardSteps.AssertSucceededAsync(await send(group.Owner.Client));

            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }
    }
}
