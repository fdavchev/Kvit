using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Xunit;
using static Kvit.Domain.Tests.MoneyRules.MoneyTestData;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class ExactSplitTests
    {
        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void AmountsThatMatchTheTotal_AreUsedAsTyped(Currency currency)
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Exact,
                Steps(3000, currency),
                Filip,
                new SplitInput(Filip, 1000 * Step(currency)),
                new SplitInput(Ana, 1500 * Step(currency)),
                new SplitInput(Marko, 500 * Step(currency)));

            Assert.Equal([1000, 1500, 500], StepsOf(shares, currency));
        }

        [Fact]
        public void TwoSevenHundredOfThreeThousand_FailsWithThreeHundredLeftToAssign()
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(
                SplitType.Exact,
                Steps(3000, Currency.MKD),
                Filip,
                [new SplitInput(Filip, 120000), new SplitInput(Ana, 150000)]);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.EXPENSE_SPLIT_DOES_NOT_ADD_UP, result.ErrorCode);
            Assert.Equal("Split adds up to 2,700 of 3,000 MKD; 300 left to assign.", result.Error);
        }

        [Fact]
        public void AmountsOverTheTotal_FailsNamingHowMuchTooMuch()
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(
                SplitType.Exact,
                Amount(3000, Currency.EUR),
                Filip,
                [new SplitInput(Filip, 2000), new SplitInput(Ana, 1300)]);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.EXPENSE_SPLIT_DOES_NOT_ADD_UP, result.ErrorCode);
            Assert.Equal("Split adds up to 33.00 of 30.00 EUR; 3.00 over the total.", result.Error);
        }

        [Fact]
        public void MkdAmountNotOnWholeDenar_FailsWithNotOnStep()
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(
                SplitType.Exact,
                Steps(1000, Currency.MKD),
                Filip,
                [new SplitInput(Filip, 49950), new SplitInput(Ana, 50050)]);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.MONEY_NOT_ON_CURRENCY_STEP, result.ErrorCode);
        }

        [Fact]
        public void PayerNotInTheSplit_StillUsesTheTypedAmounts()
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Exact,
                Amount(1001, Currency.EUR),
                Outsider,
                new SplitInput(Ana, 500),
                new SplitInput(Marko, 501));

            Assert.Equal([500, 501], ShareAmounts(shares));
        }
    }
}
