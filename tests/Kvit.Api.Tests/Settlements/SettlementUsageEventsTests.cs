using Kvit.Api.Tests.Expenses;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class SettlementUsageEventsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Record_ConfirmedAtOnce_WritesOneUsageEventForTheCaller()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await SettlementSteps.ConfirmedBojanToAnaAsync(group);

            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Record_ConfirmedAtOnce_WritesTheUsageEventWithNoDetailAndTheClockTime()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await SettlementSteps.ConfirmedBojanToAnaAsync(group);

            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await UsageValueAsync(group.Owner.UserId, "occurred_at AT TIME ZONE 'UTC'"));
            Assert.Null(await UsageValueAsync(group.Owner.UserId, "detail"));
        }

        [Fact]
        public async Task Record_ByTheOwnerForAPlainNameReceiver_WritesTheUsageEventForTheOwner()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.MarkoRowId);

            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
        }

        [Fact]
        public async Task Record_Pending_WritesNoUsageEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await SettlementSteps.PendingBojanToAnaAsync(group);

            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Member.UserId));
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
        }

        [Fact]
        public async Task Confirm_OfAPendingOne_WritesOneUsageEventForTheConfirmerAndNoneForThePayer()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);

            await SettlementSteps.ConfirmAsync(group.Third, group.GroupId, settlementId);

            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Third.UserId));
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Confirm_ByTheOwnerForAPlainNameReceiver_WritesTheUsageEventForTheOwner()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);

            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);

            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
        }

        [Fact]
        public async Task RejectCancelAndFailedActions_WriteNoUsageEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid rejected = await SettlementSteps.PendingBojanToAnaAsync(group);
            Guid cancelled = await SettlementSteps.PendingBojanToAnaAsync(group);
            Guid forbidden = await SettlementSteps.PendingBojanToAnaAsync(group);

            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, rejected);
            await SettlementSteps.CancelAsync(group.Member, group.GroupId, cancelled);
            await SettlementRequests.ConfirmAsync(group.Third.Client, group.GroupId, forbidden);
            await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, rejected);

            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Member.UserId));
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Third.UserId));
        }

        private async Task<string?> UsageValueAsync(Guid userId, string expression)
        {
            List<string?> values = await _app.QueryAsync(
                $"SELECT ({expression})::text FROM usage_events WHERE user_id = @user_id AND type = 'SettlementConfirmed'",
                new NpgsqlParameter("user_id", userId));

            return Assert.Single(values);
        }
    }
}
