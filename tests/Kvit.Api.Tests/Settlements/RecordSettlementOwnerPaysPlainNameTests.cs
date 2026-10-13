using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class RecordSettlementOwnerPaysPlainNameTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Theory]
        [InlineData(ExpenseInputs.Mkd, 30_000)]
        [InlineData(ExpenseInputs.Eur, 1_550)]
        public async Task Record_ByTheOwnerAsThePayerToAPlainName_Answers200WithAConfirmedSettlementResolvedAtCreationTime(string currency, long amountMinor)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.OwnerRowId, group.MarkoRowId, amountMinor, currency);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(SettlementJson.FieldNames, GroupRequests.PropertyNamesOf(body));
            Assert.Equal("Confirmed", body.GetProperty("status").GetString());
            Assert.Equal(_app.Clock.GetUtcNow(), body.GetProperty("createdAt").GetDateTimeOffset());
            Assert.Equal(_app.Clock.GetUtcNow(), body.GetProperty("resolvedAt").GetDateTimeOffset());
            Assert.Equal(group.Owner.UserId, body.GetProperty("recordedByUserId").GetGuid());
            Assert.Equal(group.OwnerRowId, body.GetProperty("fromMemberId").GetGuid());
            Assert.Equal("Ana", body.GetProperty("fromName").GetString());
            Assert.Equal(group.MarkoRowId, body.GetProperty("toMemberId").GetGuid());
            Assert.Equal("Marko", body.GetProperty("toName").GetString());
            Assert.Equal(amountMinor, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal(currency, body.GetProperty("currency").GetString());
        }

        [Fact]
        public async Task Record_ByTheOwnerAsThePayerToAPlainName_StoresAConfirmedRowResolvedByTheOwnerAtTheCreationTime()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            _app.Clock.Advance(OneMinute);

            Guid settlementId = await SettlementSteps.ConfirmedAnaToMarkoAsync(group);

            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "recorded_by_user_id"));
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "resolved_by_user_id"));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await SettlementRows.ValueAsync(_app, settlementId, "resolved_at AT TIME ZONE 'UTC'"));
            Assert.Equal(await SettlementRows.ValueAsync(_app, settlementId, "created_at"), await SettlementRows.ValueAsync(_app, settlementId, "resolved_at"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
        }

        [Theory]
        [InlineData(ExpenseInputs.Mkd, 30_000)]
        [InlineData(ExpenseInputs.Eur, 1_550)]
        public async Task Record_ByTheOwnerAsThePayerToAPlainName_MovesTheBalancesAtOnceAndEveryCurrencyStillSumsToZero(string currency, long amountMinor)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.OwnerRowId, group.MarkoRowId, amountMinor, currency);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            JsonElement entry = BalanceJson.CurrencyOf(body, currency);
            Assert.Equal([amountMinor, 0L, -amountMinor, 0L, 0L], BalanceJson.BalanceMinors(entry));
            Assert.Equal([new ExpectedPayment(group.MarkoRowId, group.OwnerRowId, amountMinor)], BalanceJson.PaymentsOf(entry));
            BalanceJson.AssertEveryCurrencySumsToZero(body);
        }

        [Fact]
        public async Task Record_ByTheOwnerAsThePayerToAPlainName_WritesOnlyTheRecordedActivityEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            Guid settlementId = await SettlementSteps.ConfirmedAnaToMarkoAsync(group);

            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementRecorded"));
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.EventValueAsync(_app, settlementId, "SettlementRecorded", "actor_user_id"));
            Assert.Equal(0, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementConfirmed"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "SettlementConfirmed"));
            Assert.Equal(2, await GroupRows.CountEventsAsync(_app, group.GroupId));
            Assert.Equal(["SettlementRecorded", "GroupCreated"], ExpenseJson.TypesOf(await SettlementSteps.ReadActivityAsync(group.Third, group.GroupId)));
        }

        [Fact]
        public async Task Record_ByTheOwnerAsThePayerToAPlainName_WritesOneConfirmedUsageEventForTheOwner()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await SettlementSteps.ConfirmedAnaToMarkoAsync(group);

            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Member.UserId));
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Third.UserId));
        }

        [Fact]
        public async Task List_AfterTheOwnerPaidAPlainName_ShowsItConfirmedWithResolvedAtAndNothingPending()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedAnaToMarkoAsync(group);

            JsonElement settlements = await SettlementSteps.ReadListAsync(group.Third, group.GroupId);

            Assert.Equal([settlementId], SettlementJson.IdsOf(settlements));
            Assert.Equal(["Confirmed"], SettlementJson.StatusesOf(settlements));
            Assert.Equal(_app.Clock.GetUtcNow(), SettlementJson.RowWithId(settlements, settlementId).GetProperty("resolvedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Record_ByTheOwnerPayingWhatSheOwesAPlainName_MakesEveryoneKvitAtOnce()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(60_000, ExpenseInputs.Mkd, group.MarkoRowId, group.MarkoRowId, group.OwnerRowId));
            JsonElement before = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.False(before.GetProperty("isEveryoneKvit").GetBoolean());

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.OwnerRowId, group.MarkoRowId, 30_000);

            JsonElement after = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([0L, 0L, 0L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(after, "MKD")));
            Assert.True(after.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Confirm_OwnerPaymentToAPlainNameThatWasConfirmedAtOnce_Answers400NotPendingAndWritesNoSecondUsageEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedAnaToMarkoAsync(group);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
        }

        [Fact]
        public async Task Reject_OwnerPaymentToAPlainNameThatWasConfirmedAtOnce_Answers400NotPendingAndKeepsItConfirmed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedAnaToMarkoAsync(group);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
        }

        [Fact]
        public async Task Cancel_OwnerPaymentToAPlainNameThatWasConfirmedAtOnce_Answers400NotPendingAndKeepsItConfirmed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedAnaToMarkoAsync(group);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal([30_000L, 0L, -30_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Delete_ByTheOwnerWhoRecordedAPaymentToAPlainName_Answers204AndTakesItOutOfTheBalances()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedAnaToMarkoAsync(group);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "deleted_by_user_id"));
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Restore_ByTheOwnerAfterDeletingAPaymentToAPlainName_Answers204AndCountsItAgain()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedAnaToMarkoAsync(group);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Equal([30_000L, 0L, -30_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Delete_ByAMemberWhoIsNeitherTheRecorderNorTheOwner_Answers403SettlementNotAllowed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedAnaToMarkoAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Record_SameClientRequestIdTwiceByTheOwnerPayingAPlainName_AnswersTheFirstConfirmedSettlementAndWritesOneOfEverything()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();

            HttpResponseMessage first = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.OwnerRowId, group.MarkoRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);
            _app.Clock.Advance(OneMinute);
            HttpResponseMessage second = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.OwnerRowId, group.MarkoRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            JsonElement firstBody = await AuthRequests.ReadJsonAsync(first);
            JsonElement secondBody = await AuthRequests.ReadJsonAsync(second);
            Assert.Equal("Confirmed", firstBody.GetProperty("status").GetString());
            Assert.Equal(firstBody.GetRawText(), secondBody.GetRawText());
            Guid settlementId = firstBody.GetProperty("id").GetGuid();
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementRecorded"));
            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
            Assert.Equal([30_000L, 0L, -30_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }
    }
}
