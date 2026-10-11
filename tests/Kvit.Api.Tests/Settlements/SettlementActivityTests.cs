using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class SettlementActivityTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public async Task Record_AsPayer_WritesOneRecordedEventByTheCallerAboutTheSettlementAndNoConfirmedEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);

            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementRecorded"));
            Assert.Equal(group.Member.UserId.ToString(), await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRecorded", "actor_user_id"));
            Assert.Equal(group.GroupId.ToString(), await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRecorded", "group_id"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "SettlementRecorded"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "SettlementConfirmed"));
        }

        [Fact]
        public async Task Record_AsReceiverWhichIsConfirmedAtOnce_WritesOnlyTheRecordedEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);

            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementRecorded"));
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRecorded", "actor_user_id"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "SettlementConfirmed"));
            Assert.Equal(2, await GroupRows.CountEventsAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Record_EventData_HasTheAmountCurrencyMemberIdsAndNamesAtThatTime()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.MemberRowId, 1_550, ExpenseInputs.Eur);

            Assert.Equal("1550", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRecorded", "data->>'amountMinor'"));
            Assert.Equal("EUR", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRecorded", "data->>'currency'"));
            Assert.Equal(group.MarkoRowId.ToString(), await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRecorded", "data->>'fromMemberId'"));
            Assert.Equal(group.MemberRowId.ToString(), await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRecorded", "data->>'toMemberId'"));
            Assert.Equal("Marko", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRecorded", "data->>'fromName'"));
            Assert.Equal("Bojan", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRecorded", "data->>'toName'"));
        }

        [Theory]
        [InlineData("confirm", "SettlementConfirmed")]
        [InlineData("reject", "SettlementRejected")]
        public async Task Resolve_ByTheReceiver_WritesOneEventOfTheMatchingTypeByTheReceiverWithTheSameData(string action, string eventType)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            _app.Clock.Advance(OneMinute);

            HttpResponseMessage response = action == "confirm"
                ? await SettlementRequests.ConfirmAsync(group.Third.Client, group.GroupId, settlementId)
                : await SettlementRequests.RejectAsync(group.Third.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, eventType));
            Assert.Equal(group.Third.UserId.ToString(), await SettlementRows.EventValueAsync(_app, settlementId, eventType, "actor_user_id"));
            Assert.Equal("30000", await SettlementRows.EventValueAsync(_app, settlementId, eventType, "data->>'amountMinor'"));
            Assert.Equal("MKD", await SettlementRows.EventValueAsync(_app, settlementId, eventType, "data->>'currency'"));
            Assert.Equal(group.MemberRowId.ToString(), await SettlementRows.EventValueAsync(_app, settlementId, eventType, "data->>'fromMemberId'"));
            Assert.Equal(group.ThirdRowId.ToString(), await SettlementRows.EventValueAsync(_app, settlementId, eventType, "data->>'toMemberId'"));
            Assert.Equal("Bojan", await SettlementRows.EventValueAsync(_app, settlementId, eventType, "data->>'fromName'"));
            Assert.Equal("Dime", await SettlementRows.EventValueAsync(_app, settlementId, eventType, "data->>'toName'"));
        }

        [Fact]
        public async Task Cancel_WritesOneCancelledEventByThePayerWithTheSameData()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);

            await SettlementSteps.CancelAsync(group.Member, group.GroupId, settlementId);

            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementCancelled"));
            Assert.Equal(group.Member.UserId.ToString(), await SettlementRows.EventValueAsync(_app, settlementId, "SettlementCancelled", "actor_user_id"));
            Assert.Equal("30000", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementCancelled", "data->>'amountMinor'"));
            Assert.Equal("Bojan", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementCancelled", "data->>'fromName'"));
            Assert.Equal("Ana", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementCancelled", "data->>'toName'"));
        }

        [Fact]
        public async Task Delete_And_Restore_WriteOneEventEachByWhoeverActedWithTheSameData()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId, 1_550, ExpenseInputs.Eur);

            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);
            await SettlementSteps.RestoreAsync(group.Member, group.GroupId, settlementId);

            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementDeleted"));
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.EventValueAsync(_app, settlementId, "SettlementDeleted", "actor_user_id"));
            Assert.Equal("1550", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementDeleted", "data->>'amountMinor'"));
            Assert.Equal("EUR", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementDeleted", "data->>'currency'"));
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementRestored"));
            Assert.Equal(group.Member.UserId.ToString(), await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRestored", "actor_user_id"));
            Assert.Equal("1550", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRestored", "data->>'amountMinor'"));
            Assert.Equal("EUR", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRestored", "data->>'currency'"));
            Assert.Equal("Ana", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRestored", "data->>'fromName'"));
            Assert.Equal("Bojan", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRestored", "data->>'toName'"));
        }

        [Fact]
        public async Task Activity_ShowsTheWholeLifeOfASettlementNewestFirstWithTheActors()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            _app.Clock.Advance(OneMinute);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            _app.Clock.Advance(OneMinute);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);
            _app.Clock.Advance(OneMinute);
            await SettlementSteps.DeleteAsync(group.Member, group.GroupId, settlementId);
            _app.Clock.Advance(OneMinute);
            await SettlementSteps.RestoreAsync(group.Owner, group.GroupId, settlementId);

            JsonElement events = await SettlementSteps.ReadActivityAsync(group.Third, group.GroupId);

            Assert.Equal(["SettlementRestored", "SettlementDeleted", "SettlementConfirmed", "SettlementRecorded", "GroupCreated"], ExpenseJson.TypesOf(events));
            Assert.Equal(
                ["Ana", "Bojan", "Ana", "Bojan", "Ana"],
                [.. events.EnumerateArray().Select(entry => entry.GetProperty("actorName").GetString()!)]);
            Assert.Equal(
                [group.Owner.UserId, group.Member.UserId, group.Owner.UserId, group.Member.UserId, group.Owner.UserId],
                [.. events.EnumerateArray().Select(entry => entry.GetProperty("actorUserId").GetGuid())]);
        }

        [Fact]
        public async Task Activity_SettlementEvent_AnswersTheDataObjectWithExactlyTheDocumentedFieldsAndNoChanges()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.MemberRowId, 1_550, ExpenseInputs.Eur);

            JsonElement events = await SettlementSteps.ReadActivityAsync(group.Member, group.GroupId);

            JsonElement entry = events[0];
            Assert.Equal("SettlementRecorded", entry.GetProperty("type").GetString());
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("changes").ValueKind);
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("expenseId").ValueKind);
            JsonElement data = entry.GetProperty("data");
            Assert.Equal(SettlementJson.EventDataFieldNames, GroupRequests.PropertyNamesOf(data));
            Assert.Equal(1_550, data.GetProperty("amountMinor").GetInt64());
            Assert.Equal("EUR", data.GetProperty("currency").GetString());
            Assert.Equal(group.MarkoRowId, data.GetProperty("fromMemberId").GetGuid());
            Assert.Equal(group.MemberRowId, data.GetProperty("toMemberId").GetGuid());
            Assert.Equal("Marko", data.GetProperty("fromName").GetString());
            Assert.Equal("Bojan", data.GetProperty("toName").GetString());
        }

        [Fact]
        public async Task Activity_EventDataKeepsTheNamesAsTheyWereWhenItHappenedAfterARename()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);

            await UserRows.SetDisplayNameAsync(_app, group.Member.UserId, "Bojan Petrov");

            Assert.Equal("Bojan", await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRecorded", "data->>'fromName'"));
        }

        [Fact]
        public async Task FailedActions_WriteNoEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid pending = await SettlementSteps.PendingBojanToAnaAsync(group);
            Guid confirmed = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            int eventsBefore = await GroupRows.CountEventsAsync(_app, group.GroupId);

            await SettlementRequests.RecordAsync(group.Third.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd);
            await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 0, ExpenseInputs.Mkd);
            await SettlementRequests.ConfirmAsync(group.Member.Client, group.GroupId, pending);
            await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, confirmed);
            await SettlementRequests.RejectAsync(group.Third.Client, group.GroupId, pending);
            await SettlementRequests.CancelAsync(group.Owner.Client, group.GroupId, pending);
            await SettlementRequests.DeleteAsync(group.Third.Client, group.GroupId, confirmed);
            await SettlementRequests.DeleteAsync(group.Member.Client, group.GroupId, pending);
            await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, confirmed);
            await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, Guid.CreateVersion7());

            Assert.Equal(eventsBefore, await GroupRows.CountEventsAsync(_app, group.GroupId));
            Assert.Equal(2, await SettlementRows.CountAsync(_app, group.GroupId));
        }
    }
}
