using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Balances
{
    public class GetBalancesSettlementsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_ConfirmedSettlement_LowersWhatTheSenderOwesAndWhatTheReceiverIsOwed()
        {
            ExpenseGroup group = await CreateGroupWhereBojanOwesAnaThirtyThousandAsync();

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 10_000);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal([20_000L, -20_000L, 0L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal([new ExpectedPayment(group.MemberRowId, group.OwnerRowId, 20_000)], BalanceJson.PaymentsOf(mkd));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_ConfirmedSettlementThatCoversTheWholeDebt_AnswersAllZeroNoPaymentsAndEveryoneIsKvit()
        {
            ExpenseGroup group = await CreateGroupWhereBojanOwesAnaThirtyThousandAsync();

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal([0L, 0L, 0L], BalanceJson.BalanceMinors(mkd));
            Assert.Empty(BalanceJson.PaymentsOf(mkd));
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_SettlementLargerThanTheDebt_FlipsWhoOwesWhomAndIsNotKvit()
        {
            ExpenseGroup group = await CreateGroupWhereBojanOwesAnaThirtyThousandAsync();

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 50_000);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal([-20_000L, 20_000L, 0L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal([new ExpectedPayment(group.OwnerRowId, group.MemberRowId, 20_000)], BalanceJson.PaymentsOf(mkd));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_SeveralPartialConfirmedSettlements_AddUp()
        {
            ExpenseGroup group = await CreateGroupWhereBojanOwesAnaThirtyThousandAsync();

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 10_000);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 5_000);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 15_000);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([0L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_PendingSettlement_DoesNotChangeTheBalancesOrThePayments()
        {
            ExpenseGroup group = await CreateGroupWhereBojanOwesAnaThirtyThousandAsync();

            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 10_000);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal([30_000L, -30_000L, 0L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal([new ExpectedPayment(group.MemberRowId, group.OwnerRowId, 30_000)], BalanceJson.PaymentsOf(mkd));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_PendingSettlementWhileEveryBalanceIsZero_AnswersZerosButEveryoneIsNotKvit()
        {
            ExpenseGroup group = await CreateGroupWhereEveryoneIsPaidUpAsync();
            JsonElement before = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.True(before.GetProperty("isEveryoneKvit").GetBoolean());

            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 10_000);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([0L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_PendingSettlementThatIsLaterConfirmed_ThenCountsInTheBalances()
        {
            ExpenseGroup group = await CreateGroupWhereBojanOwesAnaThirtyThousandAsync();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000);

            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([0L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_RejectedSettlement_DoesNotCountAndNoLongerBlocksKvit()
        {
            ExpenseGroup group = await CreateGroupWhereEveryoneIsPaidUpAsync();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 10_000);

            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, settlementId);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([0L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_CancelledSettlement_DoesNotCountAndNoLongerBlocksKvit()
        {
            ExpenseGroup group = await CreateGroupWhereEveryoneIsPaidUpAsync();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 10_000);

            await SettlementSteps.CancelAsync(group.Member, group.GroupId, settlementId);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([0L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_RejectedAndCancelledSettlementsOnADebt_LeaveTheDebtUntouched()
        {
            ExpenseGroup group = await CreateGroupWhereBojanOwesAnaThirtyThousandAsync();
            Guid rejected = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 10_000);
            Guid cancelled = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 20_000);
            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, rejected);
            await SettlementSteps.CancelAsync(group.Member, group.GroupId, cancelled);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal([30_000L, -30_000L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_DeletedConfirmedSettlement_NoLongerCounts()
        {
            ExpenseGroup group = await CreateGroupWhereBojanOwesAnaThirtyThousandAsync();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000);

            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([30_000L, -30_000L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_RestoredSettlement_CountsAgain()
        {
            ExpenseGroup group = await CreateGroupWhereBojanOwesAnaThirtyThousandAsync();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            await SettlementSteps.RestoreAsync(group.Owner, group.GroupId, settlementId);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([0L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_MkdAndEurSettlements_AreCountedPerCurrencyAndNeverMixed()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(60_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_000, ExpenseInputs.Eur, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 500, ExpenseInputs.Eur);

            JsonElement afterEur = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([30_000L, -30_000L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(afterEur, "MKD")));
            Assert.Equal([0L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(afterEur, "EUR")));
            Assert.False(afterEur.GetProperty("isEveryoneKvit").GetBoolean());

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd);

            JsonElement afterBoth = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([0L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(afterBoth, "MKD")));
            Assert.Equal([0L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(afterBoth, "EUR")));
            Assert.True(afterBoth.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_PendingSettlementInOneCurrency_BlocksKvitEvenWhenTheOtherCurrencyIsPaidUp()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_000, ExpenseInputs.Eur, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 500, ExpenseInputs.Eur);

            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 10_000, ExpenseInputs.Mkd);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([0L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "EUR")));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_SettlementsOnlyAndNoExpenses_AnswersTheCurrencyWithTheMovedMoney()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 10_000, ExpenseInputs.Eur);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal(["EUR"], BalanceJson.CurrencyCodesOf(body));
            JsonElement eur = BalanceJson.CurrencyOf(body, "EUR");
            Assert.Equal([-10_000L, 10_000L], BalanceJson.BalanceMinors(eur));
            Assert.Equal([new ExpectedPayment(group.OwnerRowId, group.MemberRowId, 10_000)], BalanceJson.PaymentsOf(eur));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_SettlementsBetweenPlainNamesSetByTheOwner_CountForThosePlainNames()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko", "Cveta");
            Guid marko = group.PlainRowIds[0];
            Guid cveta = group.PlainRowIds[1];
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(60_000, ExpenseInputs.Mkd, marko, marko, cveta));

            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, cveta, marko, 30_000);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([0L, 0L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_SettlementOfAMemberWhoLeftAfterwards_StillCountsAndTheMemberStaysInTheAnswerAsNotCurrent()
        {
            ExpenseGroup group = await CreateGroupWhereBojanOwesAnaThirtyThousandAsync();
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Left");

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal([0L, 0L, 0L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal([true, false, true], BalanceJson.BalanceIsCurrents(mkd));
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        private async Task<ExpenseGroup> CreateGroupWhereBojanOwesAnaThirtyThousandAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(60_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));

            return group;
        }

        private async Task<ExpenseGroup> CreateGroupWhereEveryoneIsPaidUpAsync()
        {
            ExpenseGroup group = await CreateGroupWhereBojanOwesAnaThirtyThousandAsync();
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000);

            return group;
        }
    }
}
