using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class ListSettlementsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public async Task List_GroupWithoutSettlements_AnswersOnlyAnEmptySettlementsArray()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.ListAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(SettlementJson.ListFieldNames, GroupRequests.PropertyNamesOf(body));
            Assert.Equal(0, body.GetProperty("settlements").GetArrayLength());
        }

        [Fact]
        public async Task List_Row_AnswersExactlyTheDocumentedFieldsWithNamesAmountAndStatus()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 1_550, ExpenseInputs.Eur);

            JsonElement row = SettlementJson.RowWithId(await SettlementSteps.ReadListAsync(group.Third, group.GroupId), settlementId);

            Assert.Equal(SettlementJson.FieldNames, GroupRequests.PropertyNamesOf(row));
            Assert.Equal(group.MemberRowId, row.GetProperty("fromMemberId").GetGuid());
            Assert.Equal("Bojan", row.GetProperty("fromName").GetString());
            Assert.Equal(group.OwnerRowId, row.GetProperty("toMemberId").GetGuid());
            Assert.Equal("Ana", row.GetProperty("toName").GetString());
            Assert.Equal(1_550, row.GetProperty("amountMinor").GetInt64());
            Assert.Equal("EUR", row.GetProperty("currency").GetString());
            Assert.Equal("Pending", row.GetProperty("status").GetString());
            Assert.Equal(group.Member.UserId, row.GetProperty("recordedByUserId").GetGuid());
            Assert.Equal(_app.Clock.GetUtcNow(), row.GetProperty("createdAt").GetDateTimeOffset());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("resolvedAt").ValueKind);
        }

        [Fact]
        public async Task List_AfterAResolution_AnswersTheResolvedAtFromTheClock()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            _app.Clock.Advance(OneMinute);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);

            JsonElement row = SettlementJson.RowWithId(await SettlementSteps.ReadListAsync(group.Owner, group.GroupId), settlementId);

            Assert.Equal("Confirmed", row.GetProperty("status").GetString());
            Assert.Equal(_app.Clock.GetUtcNow(), row.GetProperty("resolvedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task List_Settlements_AreNewestFirst()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid oldest = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            _app.Clock.Advance(OneMinute);
            Guid middle = await SettlementSteps.PendingBojanToDimeAsync(group);
            _app.Clock.Advance(OneMinute);
            Guid newest = await SettlementSteps.PendingBojanToMarkoAsync(group);

            JsonElement settlements = await SettlementSteps.ReadListAsync(group.Owner, group.GroupId);

            Assert.Equal([newest, middle, oldest], SettlementJson.IdsOf(settlements));
        }

        [Fact]
        public async Task List_AnswersEveryStatusPendingConfirmedRejectedAndCancelled()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid confirmed = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            _app.Clock.Advance(OneMinute);
            Guid rejected = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, rejected);
            _app.Clock.Advance(OneMinute);
            Guid cancelled = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.CancelAsync(group.Member, group.GroupId, cancelled);
            _app.Clock.Advance(OneMinute);
            Guid pending = await SettlementSteps.PendingBojanToAnaAsync(group);

            JsonElement settlements = await SettlementSteps.ReadListAsync(group.Third, group.GroupId);

            Assert.Equal([pending, cancelled, rejected, confirmed], SettlementJson.IdsOf(settlements));
            Assert.Equal(["Pending", "Cancelled", "Rejected", "Confirmed"], SettlementJson.StatusesOf(settlements));
        }

        [Fact]
        public async Task List_LeavesOutDeletedSettlements()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid kept = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            _app.Clock.Advance(OneMinute);
            Guid deleted = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, deleted);

            JsonElement settlements = await SettlementSteps.ReadListAsync(group.Owner, group.GroupId);

            Assert.Equal([kept], SettlementJson.IdsOf(settlements));
        }

        [Fact]
        public async Task List_KeepsTheNamesOfMembersWhoLeftTheGroup()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Left");

            JsonElement row = SettlementJson.RowWithId(await SettlementSteps.ReadListAsync(group.Owner, group.GroupId), settlementId);

            Assert.Equal("Bojan", row.GetProperty("fromName").GetString());
            Assert.Equal("Ana", row.GetProperty("toName").GetString());
        }

        [Fact]
        public async Task List_NamesFollowTheCurrentDisplayNameOfAccountMembersAndThePlainNameOfOthers()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId);
            await UserRows.SetDisplayNameAsync(_app, group.Owner.UserId, "Ana Ivanova");

            JsonElement row = SettlementJson.RowWithId(await SettlementSteps.ReadListAsync(group.Member, group.GroupId), settlementId);

            Assert.Equal("Marko", row.GetProperty("fromName").GetString());
            Assert.Equal("Ana Ivanova", row.GetProperty("toName").GetString());
        }

        [Fact]
        public async Task List_ByAnyCurrentMember_AnswersTheSameForEveryone()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            _app.Clock.Advance(OneMinute);
            await SettlementSteps.PendingBojanToDimeAsync(group);

            string forOwner = await ReadTextAsync(group.Owner, group.GroupId);
            string forMember = await ReadTextAsync(group.Member, group.GroupId);
            string forThird = await ReadTextAsync(group.Third, group.GroupId);

            Assert.Equal(forOwner, forMember);
            Assert.Equal(forOwner, forThird);
        }

        [Fact]
        public async Task List_SettlementsOfAnotherGroup_AreNotMixedIn()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SettlementGroup other = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.ConfirmedBojanToAnaAsync(other);

            JsonElement settlements = await SettlementSteps.ReadListAsync(group.Owner, group.GroupId);

            Assert.Equal(0, settlements.GetArrayLength());
        }

        private static async Task<string> ReadTextAsync(SignedInUser viewer, Guid groupId)
        {
            HttpResponseMessage response = await SettlementRequests.ListAsync(viewer.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }
    }
}
