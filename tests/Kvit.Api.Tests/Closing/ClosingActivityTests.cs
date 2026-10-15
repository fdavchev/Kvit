using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public class ClosingActivityTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Start_WritesOneClosingStartedEventByTheOwner()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingStarted"));
            Assert.Equal(group.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingStarted", "actor_user_id"));
            Assert.Equal(group.GroupId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingStarted", "group_id"));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingStarted", "created_at AT TIME ZONE 'UTC'"));
        }

        [Fact]
        public async Task Confirm_WritesOneClosingConfirmedEventByTheMemberAndNoFinishedEventWhileSomeoneIsWaiting()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingConfirmed"));
            Assert.Equal(group.Member.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingConfirmed", "actor_user_id"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
        }

        [Fact]
        public async Task Confirm_ByTheLastMember_WritesTheirConfirmedEventAndOneGroupFinishedEventByThem()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);

            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);

            Assert.Equal(2, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingConfirmed"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
            Assert.Equal(group.Third.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "GroupFinished", "actor_user_id"));
        }

        [Fact]
        public async Task Unlock_WritesOneGroupReopenedEventByTheOwner()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupReopened"));
            Assert.Equal(group.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "GroupReopened", "actor_user_id"));
        }

        [Fact]
        public async Task Activity_AfterAnObjectionAndANewStart_ShowsTheWholeStoryNewestFirstWithTheActors()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            _app.Clock.Advance(ClosingSteps.OneMinute);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            _app.Clock.Advance(ClosingSteps.OneMinute);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            _app.Clock.Advance(ClosingSteps.OneMinute);
            await ClosingSteps.ObjectAsync(group.Third, group.GroupId, "Not yet");
            _app.Clock.Advance(ClosingSteps.OneMinute);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            JsonElement events = await SettlementSteps.ReadActivityAsync(group.Third, group.GroupId);

            Assert.Equal(["ClosingStarted", "ClosingObjected", "ClosingConfirmed", "ClosingStarted", "GroupCreated"], ExpenseJson.TypesOf(events));
            Assert.Equal(
                ["Ana", "Dime", "Bojan", "Ana", "Ana"],
                [.. events.EnumerateArray().Select(entry => entry.GetProperty("actorName").GetString()!)]);
        }

        [Fact]
        public async Task Activity_ObjectedEvent_AnswersTheReasonInTheDataObject()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ObjectAsync(group.Member, group.GroupId, "I still owe Marko 200");

            JsonElement events = await SettlementSteps.ReadActivityAsync(group.Owner, group.GroupId);

            JsonElement entry = events[0];
            Assert.Equal("ClosingObjected", entry.GetProperty("type").GetString());
            Assert.Equal("I still owe Marko 200", entry.GetProperty("data").GetProperty("reason").GetString());
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("changes").ValueKind);
        }

        [Fact]
        public async Task Activity_WholeLifeOfAClosing_ContainsEachEventTypeTheRightNumberOfTimes()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            _app.Clock.Advance(ClosingSteps.OneMinute);
            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);

            JsonElement events = await SettlementSteps.ReadActivityAsync(group.Owner, group.GroupId);

            string[] expected = ["ClosingConfirmed", "ClosingConfirmed", "ClosingStarted", "GroupCreated", "GroupFinished", "GroupReopened"];
            string[] sortedTypes = [.. ExpenseJson.TypesOf(events).Order(StringComparer.Ordinal)];
            Assert.Equal(expected, sortedTypes);
            Assert.Equal("GroupReopened", ExpenseJson.TypesOf(events)[0]);
        }

        [Fact]
        public async Task FailedClosingActions_WriteNoEvent()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            int eventsBefore = await GroupRows.CountEventsAsync(_app, group.GroupId);

            await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);
            await ClosingRequests.StartAsync(group.Third.Client, group.GroupId);
            await ClosingRequests.ConfirmAsync(group.Owner.Client, group.GroupId);
            await ClosingRequests.ConfirmAsync(group.Member.Client, group.GroupId);
            await ClosingRequests.ObjectAsync(group.Owner.Client, group.GroupId, "No");
            await ClosingRequests.ObjectAsync(group.Third.Client, group.GroupId, ExpenseInputs.TextOfLength(201));
            await ClosingRequests.UnlockAsync(group.Owner.Client, group.GroupId);
            await ClosingRequests.UnlockAsync(group.Third.Client, group.GroupId);

            Assert.Equal(eventsBefore, await GroupRows.CountEventsAsync(_app, group.GroupId));
            Assert.Equal(1, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task ClosingActions_WriteNoUsageEvents()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            Guid[] everyone = [group.Owner.UserId, group.Member.UserId, group.Third.UserId];
            int usageEventsBefore = await ClosingRows.CountUsageEventsOfAsync(_app, everyone);

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            await ClosingSteps.ObjectAsync(group.Member, group.GroupId, "Wait");
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);
            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);

            Assert.Equal(usageEventsBefore, await ClosingRows.CountUsageEventsOfAsync(_app, everyone));
        }
    }
}
