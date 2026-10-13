using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public static class SettlementSteps
    {
        public const string ThirdName = "Dime";
        public const string MarkoName = "Marko";
        public const string CvetaName = "Cveta";

        public static async Task<SettlementGroup> CreateGroupAsync(ExpensesApp app)
        {
            ExpenseGroup group = await app.CreateExpenseGroupAsync(MarkoName, CvetaName);
            SignedInUser third = await app.SignUpAsync(ThirdName);
            DateTimeOffset thirdJoinedAt = app.Clock.GetUtcNow() + (ExpensesApp.JoiningGap * 10);
            Guid thirdRowId = await GroupRows.InsertMemberAsync(app, group.GroupId, third.UserId, ThirdName, group.Owner.UserId, joinedAt: thirdJoinedAt);

            return new SettlementGroup(
                group.GroupId,
                group.Owner,
                group.OwnerRowId,
                group.Member,
                group.MemberRowId,
                third,
                thirdRowId,
                group.PlainRowIds[0],
                group.PlainRowIds[1]);
        }

        public static async Task<Guid> RecordAsync(
            SignedInUser caller,
            Guid groupId,
            Guid fromMemberId,
            Guid toMemberId,
            long amountMinor = SettlementRequests.DefaultAmountMinor,
            string currency = ExpenseInputs.Mkd,
            Guid? clientRequestId = null)
        {
            HttpResponseMessage response = await SettlementRequests.RecordAsync(caller.Client, groupId, fromMemberId, toMemberId, amountMinor, currency, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
        }

        public static Task<Guid> PendingBojanToAnaAsync(SettlementGroup group)
        {
            return RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId);
        }

        public static Task<Guid> PendingBojanToDimeAsync(SettlementGroup group)
        {
            return RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.ThirdRowId);
        }

        public static Task<Guid> PendingBojanToMarkoAsync(SettlementGroup group)
        {
            return RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.MarkoRowId);
        }

        public static Task<Guid> PendingMarkoToBojanByTheOwnerAsync(SettlementGroup group)
        {
            return RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.MemberRowId);
        }

        public static Task<Guid> PendingAnaToBojanAsync(SettlementGroup group)
        {
            return RecordAsync(group.Owner, group.GroupId, group.OwnerRowId, group.MemberRowId);
        }

        public static Task<Guid> ConfirmedAnaToMarkoAsync(SettlementGroup group)
        {
            return RecordAsync(group.Owner, group.GroupId, group.OwnerRowId, group.MarkoRowId);
        }

        public static Task<Guid> ConfirmedBojanToAnaAsync(SettlementGroup group)
        {
            return RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId);
        }

        public static async Task ConfirmAsync(SignedInUser caller, Guid groupId, Guid settlementId)
        {
            await AssertNoContentAsync(await SettlementRequests.ConfirmAsync(caller.Client, groupId, settlementId));
        }

        public static async Task RejectAsync(SignedInUser caller, Guid groupId, Guid settlementId)
        {
            await AssertNoContentAsync(await SettlementRequests.RejectAsync(caller.Client, groupId, settlementId));
        }

        public static async Task CancelAsync(SignedInUser caller, Guid groupId, Guid settlementId)
        {
            await AssertNoContentAsync(await SettlementRequests.CancelAsync(caller.Client, groupId, settlementId));
        }

        public static async Task DeleteAsync(SignedInUser caller, Guid groupId, Guid settlementId)
        {
            await AssertNoContentAsync(await SettlementRequests.DeleteAsync(caller.Client, groupId, settlementId));
        }

        public static async Task RestoreAsync(SignedInUser caller, Guid groupId, Guid settlementId)
        {
            await AssertNoContentAsync(await SettlementRequests.RestoreAsync(caller.Client, groupId, settlementId));
        }

        public static async Task<JsonElement> ReadListAsync(SignedInUser viewer, Guid groupId)
        {
            HttpResponseMessage response = await SettlementRequests.ListAsync(viewer.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("settlements");
        }

        public static async Task<JsonElement> ReadActivityAsync(SignedInUser viewer, Guid groupId)
        {
            HttpResponseMessage response = await ExpenseRequests.ActivityAsync(viewer.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("events");
        }

        public static async Task<List<long>> BalanceMinorsAsync(SignedInUser viewer, Guid groupId, string currency)
        {
            JsonElement body = await BalanceRequests.ReadBalancedAsync(viewer.Client, groupId);

            return BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, currency));
        }

        public static async Task AssertAllBalancesZeroAsync(SignedInUser viewer, Guid groupId)
        {
            JsonElement body = await BalanceRequests.ReadBalancedAsync(viewer.Client, groupId);

            foreach (JsonElement currencyEntry in body.GetProperty("currencies").EnumerateArray())
            {
                Assert.All(BalanceJson.BalanceMinors(currencyEntry), balanceMinor => Assert.Equal(0, balanceMinor));
            }
        }

        public static async Task AssertNoContentAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
    }
}
