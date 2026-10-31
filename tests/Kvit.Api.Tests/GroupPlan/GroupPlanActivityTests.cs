using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public class GroupPlanActivityTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public async Task Put_NewPlan_WritesExactlyOneGroupPlanSetEvent()
        {
            ExpenseGroup group = await CreateGroupAsync();

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, GroupPlanSteps.SetEventType));
            Assert.Equal(2, await GroupRows.CountEventsAsync(_app, group.GroupId));
            Assert.Equal([GroupPlanSteps.SetEventType, "GroupCreated"], await GroupPlanSteps.EventTypesAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task Put_NewPlan_ShowsTheAmountAndTheCurrencyInTheDataOfTheRow()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            JsonElement events = await GroupPlanSteps.EventsAsync(group.Member, group.GroupId);

            JsonElement entry = events[0];
            Assert.Equal(GroupPlanSteps.SetEventType, entry.GetProperty("type").GetString());
            Assert.Equal(ExpenseJson.ActivityFieldNames, GroupRequests.PropertyNamesOf(entry));
            JsonElement data = entry.GetProperty("data");
            Assert.Equal(["amountMinor", "currency"], GroupRequests.PropertyNamesOf(data));
            Assert.Equal(GroupPlanSteps.SixHundredEur, data.GetProperty("amountMinor").GetInt64());
            Assert.Equal(GroupPlanRequests.Eur, data.GetProperty("currency").GetString());
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("changes").ValueKind);
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("expenseId").ValueKind);
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("memberId").ValueKind);
        }

        [Fact]
        public async Task Put_NewPlan_RecordsTheOwnerAsTheActor()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            JsonElement events = await GroupPlanSteps.EventsAsync(group.Member, group.GroupId);

            Assert.Equal(group.Owner.UserId, events[0].GetProperty("actorUserId").GetGuid());
            Assert.Equal(GroupsApp.OwnerName, events[0].GetProperty("actorName").GetString());
        }

        [Fact]
        public async Task Put_ChangedAmount_WritesAnotherGroupPlanSetEventWithTheNewValues()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            _app.Clock.Advance(OneMinute);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 300_000, GroupPlanRequests.Mkd);

            JsonElement events = await GroupPlanSteps.EventsAsync(group.Owner, group.GroupId);
            Assert.Equal([GroupPlanSteps.SetEventType, GroupPlanSteps.SetEventType, "GroupCreated"], ExpenseJson.TypesOf(events));
            Assert.Equal(300_000, events[0].GetProperty("data").GetProperty("amountMinor").GetInt64());
            Assert.Equal(GroupPlanSteps.TwoThousandMkd, events[1].GetProperty("data").GetProperty("amountMinor").GetInt64());
        }

        [Fact]
        public async Task Put_ChangedCurrencyOnly_WritesOneGroupPlanSetEventWithTheNewCurrency()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 100_000, GroupPlanRequests.Mkd);
            _app.Clock.Advance(OneMinute);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 100_000, GroupPlanRequests.Eur);

            JsonElement events = await GroupPlanSteps.EventsAsync(group.Owner, group.GroupId);
            Assert.Equal(2, ExpenseJson.TypesOf(events).Count(type => type == GroupPlanSteps.SetEventType));
            Assert.Equal(GroupPlanRequests.Eur, events[0].GetProperty("data").GetProperty("currency").GetString());
        }

        [Fact]
        public async Task Put_TheSameValueAgain_WritesNoNewEvent()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            _app.Clock.Advance(OneMinute);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, GroupPlanSteps.SetEventType));
            Assert.Equal(2, await GroupRows.CountEventsAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Delete_ExistingPlan_WritesExactlyOneGroupPlanRemovedEvent()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            _app.Clock.Advance(OneMinute);

            await GroupPlanSteps.RemoveAsync(group.Owner, group.GroupId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, GroupPlanSteps.RemovedEventType));
            Assert.Equal([GroupPlanSteps.RemovedEventType, GroupPlanSteps.SetEventType, "GroupCreated"], await GroupPlanSteps.EventTypesAsync(group.Owner, group.GroupId));
            JsonElement entry = (await GroupPlanSteps.EventsAsync(group.Member, group.GroupId))[0];
            Assert.Equal(group.Owner.UserId, entry.GetProperty("actorUserId").GetGuid());
            Assert.Equal(ExpenseJson.ActivityFieldNames, GroupRequests.PropertyNamesOf(entry));
        }

        [Fact]
        public async Task Delete_GroupWithoutAPlan_WritesNoEvent()
        {
            ExpenseGroup group = await CreateGroupAsync();

            await GroupPlanSteps.RemoveAsync(group.Owner, group.GroupId);

            Assert.Equal(1, await GroupRows.CountEventsAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, GroupPlanSteps.RemovedEventType));
        }

        [Fact]
        public async Task Delete_TwiceInARow_WritesOnlyOneGroupPlanRemovedEvent()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            await GroupPlanSteps.RemoveAsync(group.Owner, group.GroupId);
            await GroupPlanSteps.RemoveAsync(group.Owner, group.GroupId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, GroupPlanSteps.RemovedEventType));
        }

        [Fact]
        public async Task SetRemoveAndSetAgain_WriteSetRemovedSetInThatOrder()
        {
            ExpenseGroup group = await CreateGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            _app.Clock.Advance(OneMinute);
            await GroupPlanSteps.RemoveAsync(group.Owner, group.GroupId);
            _app.Clock.Advance(OneMinute);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            Assert.Equal(
                [GroupPlanSteps.SetEventType, GroupPlanSteps.RemovedEventType, GroupPlanSteps.SetEventType, "GroupCreated"],
                await GroupPlanSteps.EventTypesAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task SetAndRemove_WriteNoUsageEvent()
        {
            ExpenseGroup group = await CreateGroupAsync();
            string before = await GroupPlanSteps.UsageSnapshotAsync(_app, group.Owner, group.Member);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);
            await GroupPlanSteps.RemoveAsync(group.Owner, group.GroupId);

            Assert.Equal(before, await GroupPlanSteps.UsageSnapshotAsync(_app, group.Owner, group.Member));
        }

        [Fact]
        public async Task Set_InOneGroup_WritesNoEventInAnotherGroup()
        {
            ExpenseGroup group = await CreateGroupAsync();
            ExpenseGroup other = await CreateGroupAsync();

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            Assert.Equal(["GroupCreated"], await GroupPlanSteps.EventTypesAsync(other.Owner, other.GroupId));
        }

        private async Task<ExpenseGroup> CreateGroupAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            _app.Clock.Advance(OneMinute);

            return group;
        }
    }
}
