using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class RecordSettlementTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Record_ByThePayer_Answers200WithAPendingSettlementWithTheDocumentedFields()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Member.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(SettlementJson.FieldNames, GroupRequests.PropertyNamesOf(body));
            Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
            Assert.Equal(group.MemberRowId, body.GetProperty("fromMemberId").GetGuid());
            Assert.Equal("Bojan", body.GetProperty("fromName").GetString());
            Assert.Equal(group.OwnerRowId, body.GetProperty("toMemberId").GetGuid());
            Assert.Equal("Ana", body.GetProperty("toName").GetString());
            Assert.Equal(30_000, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", body.GetProperty("currency").GetString());
            Assert.Equal("Pending", body.GetProperty("status").GetString());
            Assert.Equal(group.Member.UserId, body.GetProperty("recordedByUserId").GetGuid());
            Assert.Equal(_app.Clock.GetUtcNow(), body.GetProperty("createdAt").GetDateTimeOffset());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("resolvedAt").ValueKind);
        }

        [Fact]
        public async Task Record_ByTheReceiver_Answers200WithAConfirmedSettlement()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Confirmed", body.GetProperty("status").GetString());
            Assert.Equal(group.Owner.UserId, body.GetProperty("recordedByUserId").GetGuid());
        }

        [Fact]
        public async Task Record_InEur_AnswersTheAmountInCentsAndTheEurCurrency()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 1_550, ExpenseInputs.Eur);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(1_550, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("EUR", body.GetProperty("currency").GetString());
            Assert.Equal([-1_550L, 1_550L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "EUR"));
        }

        [Fact]
        public async Task Record_ByThePayer_StoresOnePendingRowWithEveryColumn()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();

            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal(group.GroupId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "group_id"));
            Assert.Equal(group.MemberRowId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "from_member_id"));
            Assert.Equal(group.OwnerRowId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "to_member_id"));
            Assert.Equal("30000", await SettlementRows.ValueAsync(_app, settlementId, "amount_minor"));
            Assert.Equal("MKD", await SettlementRows.ValueAsync(_app, settlementId, "currency"));
            Assert.Equal("Pending", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(group.Member.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "recorded_by_user_id"));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await SettlementRows.ValueAsync(_app, settlementId, "created_at AT TIME ZONE 'UTC'"));
            Assert.Equal(clientRequestId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "client_request_id"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "resolved_by_user_id"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "resolved_at"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_by_user_id"));
        }

        [Fact]
        public async Task Record_ByTheReceiver_StoresAConfirmedRow()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);

            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "recorded_by_user_id"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
        }

        [Fact]
        public async Task Record_NamesFollowTheCurrentDisplayNameOfAccountMembersAndThePlainNameOfOthers()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await UserRows.SetDisplayNameAsync(_app, group.Member.UserId, "Bojan Petrov");

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.MarkoRowId, 30_000, ExpenseInputs.Mkd);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Bojan Petrov", body.GetProperty("fromName").GetString());
            Assert.Equal("Marko", body.GetProperty("toName").GetString());
        }

        [Fact]
        public async Task Record_TwoSettlementsBetweenTheSameMembersWithDifferentRequestIds_SaveTwoRows()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            await SettlementSteps.ConfirmedBojanToAnaAsync(group);

            Assert.Equal(2, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal([-60_000L, 60_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Record_AmountLargerThanAnythingTheMembersOwe_IsAllowed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 99_999_900);

            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal([-99_999_900L, 99_999_900L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Record_SmallestAmounts_AreOneDenarInMkdAndOneCentInEur()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 100, ExpenseInputs.Mkd);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 1, ExpenseInputs.Eur);

            Assert.Equal([-100L, 100L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
            Assert.Equal([-1L, 1L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "EUR"));
        }

        [Theory]
        [InlineData("MKD", 999_999_999_900)]
        [InlineData("EUR", 999_999_999_999)]
        public async Task Record_LargestAllowedAmount_Answers200AndTheBalancesStillSumToZero(string currency, long amountMinor)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, amountMinor, currency);

            Assert.Equal([-amountMinor, amountMinor, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, currency));
        }

        [Fact]
        public async Task Record_DoesNotTouchTheExpensesOfTheGroup()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            string expenseBefore = (await ExpenseRows.ExpenseValueAsync(_app, expenseId, "expenses"))!;

            await SettlementSteps.ConfirmedBojanToAnaAsync(group);

            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
            Assert.Equal(2, await ExpenseRows.CountSharesOfAsync(_app, expenseId));
            Assert.Equal(expenseBefore, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "expenses"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseAdded"));
        }
    }
}
