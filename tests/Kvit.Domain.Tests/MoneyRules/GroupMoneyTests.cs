using System.Net;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;
using static Kvit.Domain.Tests.MoneyRules.GroupMoneyTestData;
using static Kvit.Domain.Tests.MoneyRules.MoneyTestData;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class GroupMoneyTests
    {
        private static readonly Guid[] Members = [Filip, Ana, Marko];

        [Fact]
        public void CheckMemberSettled_NothingRecorded_PassesForEveryMember()
        {
            GroupMoney money = new(Members, [], []);

            Assert.All(Members, member => Assert.True(money.CheckMemberSettled(member).IsSuccess));
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_BalancesBackToZeroAfterAConfirmedPayment_PassesForEveryMember()
        {
            GroupMoney money = Settled(Members, Filip, Ana);

            Assert.All(Members, member => Assert.True(money.CheckMemberSettled(member).IsSuccess));
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_AnExpenseThatNetsToZeroForItsPayer_Passes()
        {
            BalanceExpense paidForYourself = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip);
            GroupMoney money = new(Members, [paidForYourself], []);

            Assert.True(money.CheckMemberSettled(Filip).IsSuccess);
            AssertBalancesSumToZero(money);
        }

        [Theory]
        [InlineData(Currency.MKD, true)]
        [InlineData(Currency.MKD, false)]
        [InlineData(Currency.EUR, true)]
        [InlineData(Currency.EUR, false)]
        public void CheckMemberSettled_ANonZeroBalanceOfEitherSignInOneCurrency_FailsWith400MemberBalanceNotZero(Currency currency, bool isOwed)
        {
            BalanceExpense expense = EqualExpense(Filip, Steps(1000, currency), Filip, Ana);
            GroupMoney money = new(Members, [expense], []);
            Guid member = isOwed ? Filip : Ana;

            Result result = money.CheckMemberSettled(member);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Contains(member.ToString(), result.Error, StringComparison.Ordinal);
            Assert.Equal(isOwed, BalanceOf(money, currency, member) > 0);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_ZeroInDenarsButOwedInEuros_FailsWith400MemberBalanceNotZero()
        {
            BalanceExpense denarExpense = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceSettlement denarsPaid = Settlement(Ana, Filip, Steps(500, Currency.MKD), SettlementStatus.Confirmed);
            BalanceExpense euroExpense = EqualExpense(Filip, Steps(1000, Currency.EUR), Filip, Ana);
            GroupMoney money = new(Members, [denarExpense, euroExpense], [denarsPaid]);

            Result result = money.CheckMemberSettled(Ana);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal(0, BalanceOf(money, Currency.MKD, Ana));
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_AMemberWithNoExpenseOrPaymentWhileOthersOweEachOther_Passes()
        {
            GroupMoney money = Owing(Members, Filip, Ana);

            Assert.True(money.CheckMemberSettled(Marko).IsSuccess);
            Assert.Equal(0, BalanceOf(money, Currency.MKD, Marko));
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_ANonZeroBalanceAndAPendingPayment_ReportsTheBalanceFirst()
        {
            BalanceExpense expense = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceSettlement pending = Settlement(Ana, Filip, Steps(500, Currency.MKD), SettlementStatus.Pending);
            GroupMoney money = new(Members, [expense], [pending]);

            Result result = money.CheckMemberSettled(Ana);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_APendingPaymentAlone_FailsWith400MemberHasPendingPaymentForTheSenderAndTheRecipient()
        {
            GroupMoney money = PendingPayment(Members, Ana, Marko);

            Result forSender = money.CheckMemberSettled(Ana);
            Result forRecipient = money.CheckMemberSettled(Marko);

            ResultAssert.Failed(forSender, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
            ResultAssert.Failed(forRecipient, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_APendingPaymentBetweenTwoOthers_PassesForTheThirdMember()
        {
            GroupMoney money = PendingPayment(Members, Ana, Marko);

            Assert.True(money.CheckMemberSettled(Filip).IsSuccess);
        }

        [Fact]
        public void CheckMemberSettled_APendingPaymentAfterTheBalancesReachedZero_FailsWith400MemberHasPendingPayment()
        {
            BalanceExpense expense = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceSettlement confirmed = Settlement(Ana, Filip, Steps(500, Currency.MKD), SettlementStatus.Confirmed);
            BalanceSettlement pending = Settlement(Marko, Ana, Steps(100, Currency.MKD), SettlementStatus.Pending);
            GroupMoney money = new(Members, [expense], [confirmed, pending]);

            Result result = money.CheckMemberSettled(Ana);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
            Assert.Contains(Ana.ToString(), result.Error, StringComparison.Ordinal);
            AssertBalancesSumToZero(money);
        }

        [Theory]
        [InlineData(SettlementStatus.Rejected, false)]
        [InlineData(SettlementStatus.Cancelled, false)]
        [InlineData(SettlementStatus.Rejected, true)]
        [InlineData(SettlementStatus.Cancelled, true)]
        [InlineData(SettlementStatus.Pending, true)]
        [InlineData(SettlementStatus.Confirmed, true)]
        public void CheckMemberSettled_APaymentThatIsNotLivePending_NeverBlocks(SettlementStatus status, bool isDeleted)
        {
            BalanceSettlement ignored = Settlement(Ana, Marko, Steps(500, Currency.MKD), status, isDeleted);
            GroupMoney money = new(Members, [], [ignored]);

            Assert.All(Members, member => Assert.True(money.CheckMemberSettled(member).IsSuccess));
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_AConfirmedPaymentThatSettlesTheDebt_DoesNotBlockEitherSide()
        {
            GroupMoney money = Settled(Members, Filip, Ana);

            Assert.True(money.CheckMemberSettled(Filip).IsSuccess);
            Assert.True(money.CheckMemberSettled(Ana).IsSuccess);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_AConfirmedPaymentThatSettlesOnlyPartOfTheDebt_FailsWithTheBalanceCodeNotThePendingCode()
        {
            BalanceExpense expense = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceSettlement partial = Settlement(Ana, Filip, Steps(200, Currency.MKD), SettlementStatus.Confirmed);
            GroupMoney money = new(Members, [expense], [partial]);

            Result result = money.CheckMemberSettled(Ana);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_ADeletedExpense_IsNotCounted()
        {
            BalanceExpense deletedMkd = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana) with { IsDeleted = true };
            BalanceExpense deletedEur = EqualExpense(Marko, Steps(1000, Currency.EUR), Marko, Ana) with { IsDeleted = true };
            GroupMoney money = new(Members, [deletedMkd, deletedEur], []);

            Assert.All(Members, member => Assert.True(money.CheckMemberSettled(member).IsSuccess));
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_ADeletedExpenseNextToALiveOne_CountsOnlyTheLiveOne()
        {
            BalanceExpense live = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceExpense deleted = EqualExpense(Marko, Steps(1000, Currency.MKD), Marko, Ana) with { IsDeleted = true };
            GroupMoney money = new(Members, [live, deleted], []);

            Assert.True(money.CheckMemberSettled(Marko).IsSuccess);
            Assert.False(money.CheckMemberSettled(Ana).IsSuccess);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_AFormerMemberWhoStillOwes_FailsWith400MemberBalanceNotZeroWithoutThrowing()
        {
            Guid former = Bojan;
            Guid[] membersWithFormer = [Filip, Ana, former];
            GroupMoney money = Owing(membersWithFormer, Filip, former);

            Result result = money.CheckMemberSettled(former);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.True(money.CheckMemberSettled(Ana).IsSuccess);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_AFormerMemberWhoIsSettled_PassesWithoutThrowing()
        {
            Guid former = Bojan;
            Guid[] membersWithFormer = [Filip, Ana, former];
            GroupMoney money = Settled(membersWithFormer, Filip, former);

            Assert.True(money.CheckMemberSettled(former).IsSuccess);
            Assert.True(money.CheckMemberSettled(Filip).IsSuccess);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckMemberSettled_AnUnknownMemberWhenNoMoneyIsRecorded_PassesBecauseNoBalanceIsEverLookedUp()
        {
            GroupMoney money = new(Members, [], []);

            Result result = money.CheckMemberSettled(Outsider);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void CheckMemberSettled_AnUnknownMemberWhenMoneyIsRecorded_ThrowsNamingTheMember()
        {
            GroupMoney money = Owing(Members, Filip, Ana);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => money.CheckMemberSettled(Outsider));

            Assert.Contains(Outsider.ToString(), exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void CheckEveryoneSettled_NothingRecorded_Passes()
        {
            GroupMoney money = new([], [], []);

            Assert.True(money.CheckEveryoneSettled().IsSuccess);
        }

        [Fact]
        public void CheckEveryoneSettled_BalancesBackToZeroAfterAConfirmedPayment_Passes()
        {
            GroupMoney money = Settled(Members, Filip, Ana);

            Assert.True(money.CheckEveryoneSettled().IsSuccess);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckEveryoneSettled_AnExpenseThatNetsToZeroForItsPayer_Passes()
        {
            BalanceExpense paidForYourself = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip);
            GroupMoney money = new(Members, [paidForYourself], []);

            Assert.True(money.CheckEveryoneSettled().IsSuccess);
            AssertBalancesSumToZero(money);
        }

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void CheckEveryoneSettled_ANonZeroBalanceInOneCurrency_FailsWith400GroupBalancesNotZero(Currency currency)
        {
            BalanceExpense expense = EqualExpense(Filip, Steps(1000, currency), Filip, Ana);
            GroupMoney money = new(Members, [expense], []);

            Result result = money.CheckEveryoneSettled();

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckEveryoneSettled_ZeroInDenarsButOwedInEuros_FailsWith400GroupBalancesNotZero()
        {
            BalanceExpense denarExpense = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceSettlement denarsPaid = Settlement(Ana, Filip, Steps(500, Currency.MKD), SettlementStatus.Confirmed);
            BalanceExpense euroExpense = EqualExpense(Marko, Steps(1000, Currency.EUR), Marko, Ana);
            GroupMoney money = new(Members, [denarExpense, euroExpense], [denarsPaid]);

            Result result = money.CheckEveryoneSettled();

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckEveryoneSettled_ANonZeroBalanceAndAPendingPayment_ReportsTheBalanceFirst()
        {
            BalanceExpense expense = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceSettlement pending = Settlement(Marko, Ana, Steps(100, Currency.MKD), SettlementStatus.Pending);
            GroupMoney money = new(Members, [expense], [pending]);

            Result result = money.CheckEveryoneSettled();

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckEveryoneSettled_APendingPaymentAlone_FailsWith400GroupHasPendingPayment()
        {
            GroupMoney money = PendingPayment(Members, Ana, Marko);

            Result result = money.CheckEveryoneSettled();

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_HAS_PENDING_PAYMENT);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckEveryoneSettled_APendingPaymentAfterTheBalancesReachedZero_FailsWith400GroupHasPendingPayment()
        {
            BalanceExpense expense = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceSettlement confirmed = Settlement(Ana, Filip, Steps(500, Currency.MKD), SettlementStatus.Confirmed);
            BalanceSettlement pending = Settlement(Marko, Ana, Steps(100, Currency.MKD), SettlementStatus.Pending);
            GroupMoney money = new(Members, [expense], [confirmed, pending]);

            Result result = money.CheckEveryoneSettled();

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_HAS_PENDING_PAYMENT);
            AssertBalancesSumToZero(money);
        }

        [Theory]
        [InlineData(SettlementStatus.Rejected, false)]
        [InlineData(SettlementStatus.Cancelled, false)]
        [InlineData(SettlementStatus.Rejected, true)]
        [InlineData(SettlementStatus.Cancelled, true)]
        [InlineData(SettlementStatus.Pending, true)]
        public void CheckEveryoneSettled_APaymentThatIsNotLivePending_NeverBlocks(SettlementStatus status, bool isDeleted)
        {
            BalanceSettlement ignored = Settlement(Ana, Marko, Steps(500, Currency.MKD), status, isDeleted);
            GroupMoney money = new(Members, [], [ignored]);

            Assert.True(money.CheckEveryoneSettled().IsSuccess);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckEveryoneSettled_ADeletedConfirmedPayment_IsNotCountedSoTheDebtStays()
        {
            BalanceExpense expense = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceSettlement deletedPayment = Settlement(Ana, Filip, Steps(500, Currency.MKD), SettlementStatus.Confirmed, isDeleted: true);
            GroupMoney money = new(Members, [expense], [deletedPayment]);

            Result result = money.CheckEveryoneSettled();

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckEveryoneSettled_AConfirmedPaymentThatSettlesOnlyPartOfTheDebt_FailsWithTheBalanceCodeNotThePendingCode()
        {
            BalanceExpense expense = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceSettlement partial = Settlement(Ana, Filip, Steps(200, Currency.MKD), SettlementStatus.Confirmed);
            GroupMoney money = new(Members, [expense], [partial]);

            Result result = money.CheckEveryoneSettled();

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckEveryoneSettled_ADeletedExpense_IsNotCounted()
        {
            BalanceExpense deleted = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana) with { IsDeleted = true };
            GroupMoney money = new(Members, [deleted], []);

            Assert.True(money.CheckEveryoneSettled().IsSuccess);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckEveryoneSettled_AFormerMemberWhoStillOwes_FailsWith400GroupBalancesNotZeroWithoutThrowing()
        {
            Guid[] membersWithFormer = [Filip, Ana, Bojan];
            GroupMoney money = Owing(membersWithFormer, Filip, Bojan);

            Result result = money.CheckEveryoneSettled();

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            AssertBalancesSumToZero(money);
        }

        [Fact]
        public void CheckEveryoneSettled_AFormerMemberWhoIsSettled_Passes()
        {
            Guid[] membersWithFormer = [Filip, Ana, Bojan];
            GroupMoney money = Settled(membersWithFormer, Filip, Bojan);

            Assert.True(money.CheckEveryoneSettled().IsSuccess);
            AssertBalancesSumToZero(money);
        }

        private static long BalanceOf(GroupMoney money, Currency currency, Guid memberId)
        {
            BalanceSummary summary = Balances.Calculate(money.MemberIdsInJoiningOrder, money.Expenses, money.Settlements);

            return summary.ByCurrency[currency].Single(balance => balance.MemberId == memberId).Balance.MinorUnits;
        }

        private static void AssertBalancesSumToZero(GroupMoney money)
        {
            BalanceSummary summary = Balances.Calculate(money.MemberIdsInJoiningOrder, money.Expenses, money.Settlements);

            foreach (KeyValuePair<Currency, IReadOnlyList<MemberBalance>> currencyBalances in summary.ByCurrency)
            {
                Assert.Equal(0, currencyBalances.Value.Sum(balance => balance.Balance.MinorUnits));
            }
        }
    }
}
