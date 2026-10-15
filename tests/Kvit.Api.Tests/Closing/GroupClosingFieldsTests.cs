using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public class GroupClosingFieldsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_OpenGroup_AnswersExactlyTheFifteenFieldsWithNoClosingTimesAndEmptyLists()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            JsonElement body = await ClosingSteps.ReadGroupAsync(group.Owner, group.GroupId);

            Assert.Equal(GroupRequests.GroupFieldNames, GroupRequests.PropertyNamesOf(body));
            Assert.Equal(JsonValueKind.Null, body.GetProperty("closingStartedAt").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("closingEndsAt").ValueKind);
            Assert.Equal(0, body.GetProperty("closingConfirmedBy").GetArrayLength());
            Assert.Equal(0, body.GetProperty("closingWaitingFor").GetArrayLength());
        }

        [Fact]
        public async Task Get_ClosingGroup_AnswersTheStartTimeFromTheClock()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            DateTimeOffset startedAt = _app.Clock.GetUtcNow();

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            JsonElement body = await ClosingSteps.ReadGroupAsync(group.Member, group.GroupId);
            Assert.Equal("Closing", body.GetProperty("status").GetString());
            Assert.Equal(startedAt, body.GetProperty("closingStartedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Get_ClosingGroup_AnswersAnEndTimeExactly24HoursAfterTheStart()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            JsonElement body = await ClosingSteps.ReadGroupAsync(group.Owner, group.GroupId);

            DateTimeOffset startedAt = body.GetProperty("closingStartedAt").GetDateTimeOffset();
            Assert.Equal(startedAt.AddHours(24), body.GetProperty("closingEndsAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Get_ClosingGroupBeforeAnyAnswer_ListsNobodyAsConfirmedAndEveryAccountMemberButTheOwnerAsWaiting()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            List<Guid> expectedWaitingIds = [.. new[] { group.MemberRowId, group.ThirdRowId }.Order()];

            JsonElement body = await ClosingSteps.ReadGroupAsync(group.Owner, group.GroupId);

            Assert.Empty(ClosingJson.NamesOf(body.GetProperty("closingConfirmedBy")));
            Assert.Equal(["Bojan", "Dime"], ClosingJson.NamesOf(body.GetProperty("closingWaitingFor")));
            Assert.Equal(expectedWaitingIds, ClosingJson.IdsOf(body.GetProperty("closingWaitingFor")));
        }

        [Fact]
        public async Task Get_AfterOneMemberConfirmed_MovesThatMemberFromWaitingToConfirmed()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);

            JsonElement body = await ClosingSteps.ReadGroupAsync(group.Third, group.GroupId);

            Assert.Equal(["Bojan"], ClosingJson.NamesOf(body.GetProperty("closingConfirmedBy")));
            Assert.Equal([group.MemberRowId], ClosingJson.IdsOf(body.GetProperty("closingConfirmedBy")));
            Assert.Equal(["Dime"], ClosingJson.NamesOf(body.GetProperty("closingWaitingFor")));
            Assert.Equal([group.ThirdRowId], ClosingJson.IdsOf(body.GetProperty("closingWaitingFor")));
        }

        [Fact]
        public async Task Get_WhenAWaitingMemberLeft_NoLongerListsThatMemberAsWaiting()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Third.UserId, "Left");

            JsonElement body = await ClosingSteps.ReadGroupAsync(group.Owner, group.GroupId);

            Assert.Equal(["Bojan"], ClosingJson.NamesOf(body.GetProperty("closingWaitingFor")));
        }

        [Fact]
        public async Task Get_FinishedGroup_ListsEveryAccountMemberButTheOwnerAsConfirmedAndNobodyAsWaiting()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            JsonElement body = await ClosingSteps.ReadGroupAsync(group.Owner, group.GroupId);

            Assert.Equal("Finished", body.GetProperty("status").GetString());
            Assert.Equal(["Bojan", "Dime"], ClosingJson.NamesOf(body.GetProperty("closingConfirmedBy")));
            Assert.Empty(ClosingJson.NamesOf(body.GetProperty("closingWaitingFor")));
        }

        [Fact]
        public async Task Get_AfterAnObjection_ClearsTheClosingTimesAndBothLists()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            await ClosingSteps.ObjectAsync(group.Third, group.GroupId, "Wait");

            JsonElement body = await ClosingSteps.ReadGroupAsync(group.Owner, group.GroupId);

            Assert.Equal("Open", body.GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("closingStartedAt").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("closingEndsAt").ValueKind);
            Assert.Equal(0, body.GetProperty("closingConfirmedBy").GetArrayLength());
            Assert.Equal(0, body.GetProperty("closingWaitingFor").GetArrayLength());
        }

        [Fact]
        public async Task Get_AfterAnUnlock_ClearsTheClosingTimesAndBothLists()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);

            JsonElement body = await ClosingSteps.ReadGroupAsync(group.Member, group.GroupId);

            Assert.Equal("Open", body.GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("closingStartedAt").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("closingEndsAt").ValueKind);
            Assert.Equal(0, body.GetProperty("closingConfirmedBy").GetArrayLength());
            Assert.Equal(0, body.GetProperty("closingWaitingFor").GetArrayLength());
        }
    }
}
