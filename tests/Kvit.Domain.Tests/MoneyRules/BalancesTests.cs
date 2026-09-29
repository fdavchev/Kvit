using Kvit.Domain.MoneyRules;
using Xunit;
using static Kvit.Domain.Tests.MoneyRules.MoneyTestData;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class BalancesTests
    {
        private static readonly Guid[] Members = [Filip, Ana, Marko];

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void WorkedExample_PaidMinusOwedPlusSentMinusReceived(Currency currency)
        {
            BalanceExpense hotel = EqualExpense(Filip, Steps(3000, currency), Filip, Ana, Marko);
            BalanceExpense dinner = EqualExpense(Ana, Steps(600, currency), Ana, Marko);
            BalanceSettlement markoPaidFilip = Settlement(Marko, Filip, Steps(500, currency), SettlementStatus.Confirmed);

            BalanceSummary summary = Balances.Calculate(Members, [hotel, dinner], [markoPaidFilip]);

            Assert.Equal([currency], summary.ByCurrency.Keys);
            Assert.Equal(Members, summary.ByCurrency[currency].Select(balance => balance.MemberId));
            Assert.Equal([1500, -700, -800], BalanceSteps(summary.ByCurrency[currency], currency));
        }

        [Fact]
        public void TwoCurrencies_AreKeptApart()
        {
            BalanceExpense denarExpense = EqualExpense(Filip, Steps(3000, Currency.MKD), Filip, Ana, Marko);
            BalanceExpense euroExpense = EqualExpense(Marko, Amount(1000, Currency.EUR), Filip, Marko);

            BalanceSummary summary = Balances.Calculate(Members, [denarExpense, euroExpense], []);

            Assert.Equal(2, summary.ByCurrency.Count);
            Assert.Equal([2000, -1000, -1000], BalanceSteps(summary.ByCurrency[Currency.MKD], Currency.MKD));
            Assert.Equal([-500, 0, 500], BalanceSteps(summary.ByCurrency[Currency.EUR], Currency.EUR));
            Assert.All(summary.ByCurrency[Currency.EUR], balance => Assert.Equal(Currency.EUR, balance.Balance.Currency));
        }

        [Theory]
        [InlineData(SettlementStatus.Pending, false)]
        [InlineData(SettlementStatus.Rejected, false)]
        [InlineData(SettlementStatus.Cancelled, false)]
        [InlineData(SettlementStatus.Confirmed, true)]
        public void SettlementsNotConfirmedOrDeleted_AreIgnored(SettlementStatus status, bool isDeleted)
        {
            BalanceExpense hotel = EqualExpense(Filip, Steps(3000, Currency.MKD), Filip, Ana, Marko);
            BalanceSettlement ignored = Settlement(Ana, Filip, Steps(1000, Currency.MKD), status, isDeleted);

            BalanceSummary summary = Balances.Calculate(Members, [hotel], [ignored]);

            Assert.Equal([2000, -1000, -1000], BalanceSteps(summary.ByCurrency[Currency.MKD], Currency.MKD));
        }

        [Fact]
        public void DeletedExpenses_AreIgnored()
        {
            BalanceExpense hotel = EqualExpense(Filip, Steps(3000, Currency.MKD), Filip, Ana, Marko);
            BalanceExpense deletedDinner = EqualExpense(Ana, Steps(600, Currency.MKD), Ana, Marko) with { IsDeleted = true };
            BalanceExpense deletedEuroTaxi = EqualExpense(Ana, Amount(900, Currency.EUR), Ana, Marko) with { IsDeleted = true };

            BalanceSummary summary = Balances.Calculate(Members, [hotel, deletedDinner, deletedEuroTaxi], []);

            Assert.Equal([Currency.MKD], summary.ByCurrency.Keys);
            Assert.Equal([2000, -1000, -1000], BalanceSteps(summary.ByCurrency[Currency.MKD], Currency.MKD));
        }

        [Fact]
        public void MemberWithNoActivity_IsStillListedAtZero()
        {
            BalanceExpense taxi = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Marko);

            BalanceSummary summary = Balances.Calculate(Members, [taxi], []);

            Assert.Equal(Members, summary.ByCurrency[Currency.MKD].Select(balance => balance.MemberId));
            Assert.Equal([500, 0, -500], BalanceSteps(summary.ByCurrency[Currency.MKD], Currency.MKD));
        }

        [Fact]
        public void NothingRecorded_GivesNoCurrencies()
        {
            BalanceSummary summary = Balances.Calculate(Members, [], []);

            Assert.Empty(summary.ByCurrency);
        }

        [Fact]
        public void ExpensePaidByUnknownMember_ThrowsNamingTheMember()
        {
            BalanceExpense expense = EqualExpense(Outsider, Steps(1000, Currency.MKD), Filip, Ana);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Balances.Calculate(Members, [expense], []));

            Assert.Contains(Outsider.ToString(), exception.Message);
        }

        [Fact]
        public void ShareOfUnknownMember_ThrowsNamingTheMember()
        {
            BalanceExpense expense = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Outsider);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Balances.Calculate(Members, [expense], []));

            Assert.Contains(Outsider.ToString(), exception.Message);
        }

        [Fact]
        public void SettlementWithUnknownMember_ThrowsNamingTheMember()
        {
            BalanceSettlement settlement = Settlement(Filip, Outsider, Steps(100, Currency.MKD), SettlementStatus.Confirmed);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Balances.Calculate(Members, [], [settlement]));

            Assert.Contains(Outsider.ToString(), exception.Message);
        }

        [Fact]
        public void ExpenseWhoseSharesDoNotAddUp_Throws()
        {
            BalanceExpense expense = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana) with { Amount = Steps(1200, Currency.MKD) };

            Assert.Throws<InvalidOperationException>(() => Balances.Calculate(Members, [expense], []));
        }

        [Fact]
        public void ExpenseOfZero_ThrowsNamingThePayerAndAmount()
        {
            Money zero = Steps(0, Currency.MKD);
            BalanceExpense expense = new(Filip, zero, [new SplitShare(Filip, 0, zero)], IsDeleted: false);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Balances.Calculate(Members, [expense], []));

            Assert.Contains(Filip.ToString(), exception.Message);
            Assert.Contains("is 0 minor units", exception.Message);
        }

        [Fact]
        public void NegativeExpense_ThrowsNamingThePayerAndAmount()
        {
            BalanceExpense expense = new(
                Ana,
                Steps(-1000, Currency.MKD),
                [new SplitShare(Filip, 0, Steps(-500, Currency.MKD)), new SplitShare(Ana, 0, Steps(-500, Currency.MKD))],
                IsDeleted: false);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Balances.Calculate(Members, [expense], []));

            Assert.Contains(Ana.ToString(), exception.Message);
            Assert.Contains(Steps(-1000, Currency.MKD).MinorUnits.ToString(), exception.Message);
        }

        [Fact]
        public void NegativeShare_ThrowsNamingTheMemberAndShare()
        {
            BalanceExpense expense = new(
                Filip,
                Steps(1000, Currency.MKD),
                [new SplitShare(Filip, 0, Steps(1500, Currency.MKD)), new SplitShare(Ana, 0, Steps(-500, Currency.MKD))],
                IsDeleted: false);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Balances.Calculate(Members, [expense], []));

            Assert.Contains(Ana.ToString(), exception.Message);
            Assert.Contains(Steps(-500, Currency.MKD).MinorUnits.ToString(), exception.Message);
        }

        [Theory]
        [InlineData(SettlementStatus.Confirmed)]
        [InlineData(SettlementStatus.Pending)]
        public void PaymentOfZero_Throws(SettlementStatus status)
        {
            BalanceSettlement settlement = Settlement(Ana, Filip, Steps(0, Currency.MKD), status);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Balances.Calculate(Members, [], [settlement]));

            Assert.Contains(Ana.ToString(), exception.Message);
            Assert.Contains("is 0 minor units", exception.Message);
        }

        [Theory]
        [InlineData(SettlementStatus.Confirmed)]
        [InlineData(SettlementStatus.Pending)]
        public void NegativePayment_Throws(SettlementStatus status)
        {
            BalanceSettlement settlement = Settlement(Ana, Filip, Steps(-100, Currency.MKD), status);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Balances.Calculate(Members, [], [settlement]));

            Assert.Contains(Ana.ToString(), exception.Message);
            Assert.Contains(Steps(-100, Currency.MKD).MinorUnits.ToString(), exception.Message);
        }

        [Theory]
        [InlineData(SettlementStatus.Confirmed)]
        [InlineData(SettlementStatus.Pending)]
        public void PaymentToYourself_Throws(SettlementStatus status)
        {
            BalanceSettlement settlement = Settlement(Marko, Marko, Steps(100, Currency.MKD), status);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Balances.Calculate(Members, [], [settlement]));

            Assert.Contains(Marko.ToString(), exception.Message);
            Assert.Contains("same member", exception.Message);
        }

        [Fact]
        public void DeletedImpossibleRows_AreIgnoredNotChecked()
        {
            Money zero = Steps(0, Currency.MKD);
            BalanceExpense hotel = EqualExpense(Filip, Steps(3000, Currency.MKD), Filip, Ana, Marko);
            BalanceExpense deletedZeroExpense = new(Ana, zero, [new SplitShare(Ana, 0, zero)], IsDeleted: true);
            BalanceSettlement deletedZeroPayment = Settlement(Ana, Filip, zero, SettlementStatus.Confirmed, isDeleted: true);
            BalanceSettlement deletedPaymentToYourself = Settlement(Marko, Marko, Steps(100, Currency.MKD), SettlementStatus.Pending, isDeleted: true);

            BalanceSummary summary = Balances.Calculate(Members, [hotel, deletedZeroExpense], [deletedZeroPayment, deletedPaymentToYourself]);

            Assert.Equal([2000, -1000, -1000], BalanceSteps(summary.ByCurrency[Currency.MKD], Currency.MKD));
        }

        [Fact]
        public void IsEveryoneKvit_AllZeroButAPaymentIsPending_IsFalse()
        {
            BalanceExpense taxi = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceSettlement confirmed = Settlement(Ana, Filip, Steps(500, Currency.MKD), SettlementStatus.Confirmed);
            BalanceSettlement pending = Settlement(Marko, Ana, Steps(100, Currency.MKD), SettlementStatus.Pending);

            BalanceSummary summary = Balances.Calculate(Members, [taxi], [confirmed, pending]);

            Assert.Equal([0, 0, 0], BalanceSteps(summary.ByCurrency[Currency.MKD], Currency.MKD));
            Assert.False(summary.IsEveryoneKvit);
        }

        [Fact]
        public void IsEveryoneKvit_DenarsSettledButEurosOwed_IsFalse()
        {
            BalanceExpense denarTaxi = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceSettlement denarsPaid = Settlement(Ana, Filip, Steps(500, Currency.MKD), SettlementStatus.Confirmed);
            BalanceExpense euroCoffee = EqualExpense(Marko, Amount(600, Currency.EUR), Marko, Ana);

            BalanceSummary summary = Balances.Calculate(Members, [denarTaxi, euroCoffee], [denarsPaid]);

            Assert.Equal([0, 0, 0], BalanceSteps(summary.ByCurrency[Currency.MKD], Currency.MKD));
            Assert.False(summary.IsEveryoneKvit);
        }

        [Fact]
        public void IsEveryoneKvit_EverythingSettled_IsTrue()
        {
            BalanceExpense denarTaxi = EqualExpense(Filip, Steps(1000, Currency.MKD), Filip, Ana);
            BalanceExpense euroCoffee = EqualExpense(Marko, Amount(600, Currency.EUR), Marko, Ana);
            BalanceSettlement denarsPaid = Settlement(Ana, Filip, Steps(500, Currency.MKD), SettlementStatus.Confirmed);
            BalanceSettlement eurosPaid = Settlement(Ana, Marko, Amount(300, Currency.EUR), SettlementStatus.Confirmed);
            BalanceSettlement deletedPending = Settlement(Marko, Filip, Steps(100, Currency.MKD), SettlementStatus.Pending, isDeleted: true);
            BalanceSettlement rejected = Settlement(Filip, Marko, Steps(100, Currency.MKD), SettlementStatus.Rejected);

            BalanceSummary summary = Balances.Calculate(Members, [denarTaxi, euroCoffee], [denarsPaid, eurosPaid, deletedPending, rejected]);

            Assert.True(summary.IsEveryoneKvit);
        }

        [Fact]
        public void IsEveryoneKvit_NothingRecorded_IsTrue()
        {
            BalanceSummary summary = Balances.Calculate(Members, [], []);

            Assert.True(summary.IsEveryoneKvit);
        }

        [Fact]
        public void IsEveryoneKvit_NothingCountsAndNothingPending_IsTrue()
        {
            BalanceExpense deletedDinner = EqualExpense(Ana, Steps(600, Currency.MKD), Ana, Marko) with { IsDeleted = true };
            BalanceSettlement rejected = Settlement(Filip, Marko, Steps(100, Currency.MKD), SettlementStatus.Rejected);
            BalanceSettlement cancelled = Settlement(Marko, Ana, Amount(250, Currency.EUR), SettlementStatus.Cancelled);

            BalanceSummary summary = Balances.Calculate(Members, [deletedDinner], [rejected, cancelled]);

            Assert.Empty(summary.ByCurrency);
            Assert.True(summary.IsEveryoneKvit);
        }

        [Fact]
        public void IsEveryoneKvit_NothingCountsButAPaymentIsPending_IsFalse()
        {
            BalanceSettlement pending = Settlement(Marko, Ana, Steps(100, Currency.MKD), SettlementStatus.Pending);

            BalanceSummary summary = Balances.Calculate(Members, [], [pending]);

            Assert.Empty(summary.ByCurrency);
            Assert.False(summary.IsEveryoneKvit);
        }

        private static BalanceExpense EqualExpense(Guid payer, Money amount, params Guid[] people)
        {
            IReadOnlyList<SplitShare> shares = Split(SplitType.Equal, amount, payer, [.. people.Select(person => new SplitInput(person, 0))]);
            return new BalanceExpense(payer, amount, shares, IsDeleted: false);
        }

        private static BalanceSettlement Settlement(Guid from, Guid to, Money amount, SettlementStatus status, bool isDeleted = false) =>
            new(from, to, amount, status, isDeleted);

        private static long[] BalanceSteps(IReadOnlyList<MemberBalance> balances, Currency currency) =>
            [.. balances.Select(balance => balance.Balance.MinorUnits / Step(currency))];
    }
}
