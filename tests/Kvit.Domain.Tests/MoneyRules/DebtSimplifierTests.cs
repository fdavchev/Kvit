using Kvit.Domain.MoneyRules;
using Xunit;
using static Kvit.Domain.Tests.MoneyRules.MoneyTestData;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class DebtSimplifierTests
    {
        private const int Seed = 20260929;
        private const int CaseCount = 2000;

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void WorkedExample_LargestDebtorPaysLargestCreditorFirst(Currency currency)
        {
            IReadOnlyList<MemberBalance> balances = BalancesOf(currency, (Filip, 1500), (Ana, -700), (Marko, -800));

            IReadOnlyList<Payment> payments = DebtSimplifier.Simplify(balances);

            Assert.Equal(
                [
                    new Payment(Marko, Filip, Steps(800, currency)),
                    new Payment(Ana, Filip, Steps(700, currency)),
                ],
                payments);
        }

        [Fact]
        public void CreditorsTiedOnAmount_EarlierJoinerIsPaidFirst()
        {
            IReadOnlyList<MemberBalance> balances = BalancesOf(Currency.MKD, (Filip, 100), (Ana, 100), (Marko, -200));

            IReadOnlyList<Payment> payments = DebtSimplifier.Simplify(balances);

            Assert.Equal(
                [
                    new Payment(Marko, Filip, Steps(100, Currency.MKD)),
                    new Payment(Marko, Ana, Steps(100, Currency.MKD)),
                ],
                payments);
        }

        [Fact]
        public void DebtorsTiedOnAmount_EarlierJoinerPaysFirst()
        {
            IReadOnlyList<MemberBalance> balances = BalancesOf(Currency.EUR, (Filip, -100), (Ana, 200), (Marko, -100));

            IReadOnlyList<Payment> payments = DebtSimplifier.Simplify(balances);

            Assert.Equal(
                [
                    new Payment(Filip, Ana, Steps(100, Currency.EUR)),
                    new Payment(Marko, Ana, Steps(100, Currency.EUR)),
                ],
                payments);
        }

        [Fact]
        public void SameBalances_GiveTheSamePaymentsEveryTime()
        {
            IReadOnlyList<MemberBalance> balances = BalancesOf(Currency.MKD, (Filip, 300), (Ana, -100), (Marko, 300), (Bojan, -250), (Elena, -250));

            IReadOnlyList<Payment> first = DebtSimplifier.Simplify(balances);
            IReadOnlyList<Payment> second = DebtSimplifier.Simplify(balances);

            Assert.Equal(first, second);
        }

        [Fact]
        public void FivePeople_NeedAtMostFourPaymentsAndEveryoneEndsAtZero()
        {
            IReadOnlyList<MemberBalance> balances = BalancesOf(Currency.MKD, (Filip, 300), (Ana, -100), (Marko, 300), (Bojan, -250), (Elena, -250));

            IReadOnlyList<Payment> payments = DebtSimplifier.Simplify(balances);

            Assert.InRange(payments.Count, 1, balances.Count - 1);
            Assert.All(AfterPaying(balances, payments), remaining => Assert.Equal(0, remaining));
        }

        [Fact]
        public void EveryoneAtZero_NeedsNoPayments()
        {
            IReadOnlyList<MemberBalance> balances = BalancesOf(Currency.EUR, (Filip, 0), (Ana, 0), (Marko, 0));

            IReadOnlyList<Payment> payments = DebtSimplifier.Simplify(balances);

            Assert.Empty(payments);
        }

        [Fact]
        public void NoMembers_NeedsNoPayments()
        {
            IReadOnlyList<Payment> payments = DebtSimplifier.Simplify([]);

            Assert.Empty(payments);
        }

        [Fact]
        public void BalancesNotAddingUpToZero_Throws()
        {
            IReadOnlyList<MemberBalance> balances = BalancesOf(Currency.MKD, (Filip, 500), (Ana, -400));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => DebtSimplifier.Simplify(balances));

            Assert.Contains("10000", exception.Message);
        }

        [Fact]
        public void BalancesInTwoCurrencies_Throws()
        {
            IReadOnlyList<MemberBalance> balances =
            [
                new MemberBalance(Filip, Steps(100, Currency.MKD)),
                new MemberBalance(Ana, Steps(-100, Currency.EUR)),
            ];

            Assert.Throws<InvalidOperationException>(() => DebtSimplifier.Simplify(balances));
        }

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void RandomBalances_PaymentsZeroEveryoneInAtMostNMinusOne(Currency currency)
        {
            Random random = new(Seed);

            for (int caseNumber = 0; caseNumber < CaseCount; caseNumber++)
            {
                int memberCount = random.Next(1, 11);
                long[] steps = new long[memberCount];
                for (int index = 0; index < memberCount - 1; index++)
                {
                    steps[index] = random.NextInt64(-100_000, 100_001);
                }

                steps[memberCount - 1] = -steps.Sum();
                IReadOnlyList<MemberBalance> balances = [.. steps.Select(amount => new MemberBalance(RandomMemberId(random), Steps(amount, currency)))];

                IReadOnlyList<Payment> payments = DebtSimplifier.Simplify(balances);

                Assert.True(payments.Count <= Math.Max(memberCount - 1, 0), $"Case {caseNumber}: {payments.Count} payments for {memberCount} people.");
                Assert.All(payments, payment =>
                {
                    Assert.True(payment.Amount.MinorUnits > 0);
                    Assert.Equal(currency, payment.Amount.Currency);
                    Assert.NotEqual(payment.FromMemberId, payment.ToMemberId);
                });
                Assert.All(AfterPaying(balances, payments), remaining => Assert.Equal(0, remaining));
            }
        }

        private static IReadOnlyList<MemberBalance> BalancesOf(Currency currency, params (Guid MemberId, long Steps)[] balances) =>
            [.. balances.Select(balance => new MemberBalance(balance.MemberId, Steps(balance.Steps, currency)))];

        private static long[] AfterPaying(IReadOnlyList<MemberBalance> balances, IReadOnlyList<Payment> payments)
        {
            Dictionary<Guid, long> remaining = balances.ToDictionary(balance => balance.MemberId, balance => balance.Balance.MinorUnits);
            foreach (Payment payment in payments)
            {
                remaining[payment.FromMemberId] += payment.Amount.MinorUnits;
                remaining[payment.ToMemberId] -= payment.Amount.MinorUnits;
            }

            return [.. remaining.Values];
        }
    }
}
