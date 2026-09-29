using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Xunit;
using static Kvit.Domain.Tests.MoneyRules.MoneyTestData;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class SharesSplitTests
    {
        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void OneTwoThreeSharesOfSixHundred_SplitsExactly(Currency currency)
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Shares,
                Steps(600, currency),
                Filip,
                new SplitInput(Filip, 1),
                new SplitInput(Ana, 2),
                new SplitInput(Marko, 3));

            Assert.Equal([100, 200, 300], StepsOf(shares, currency));
        }

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void OneTwoThreeSharesOfAThousand_PayerTakesTheLeftover(Currency currency)
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Shares,
                Steps(1000, currency),
                Filip,
                new SplitInput(Filip, 1),
                new SplitInput(Ana, 2),
                new SplitInput(Marko, 3));

            Assert.Equal([167, 333, 500], StepsOf(shares, currency));
        }

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void PayerWithZeroShares_StillTakesTheLeftover(Currency currency)
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Shares,
                Steps(1000, currency),
                Filip,
                new SplitInput(Filip, 0),
                new SplitInput(Ana, 1),
                new SplitInput(Marko, 1),
                new SplitInput(Bojan, 1));

            Assert.Equal([1, 333, 333, 333], StepsOf(shares, currency));
        }

        [Fact]
        public void PayerNotInTheSplit_FirstListedTakesTheLeftover()
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Shares,
                Amount(1000, Currency.EUR),
                Outsider,
                new SplitInput(Ana, 1),
                new SplitInput(Marko, 1),
                new SplitInput(Bojan, 1));

            Assert.Equal([334, 333, 333], ShareAmounts(shares));
        }

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void AllSharesZero_FailsWithNoShares(Currency currency)
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(
                SplitType.Shares,
                Steps(1000, currency),
                Filip,
                [new SplitInput(Filip, 0), new SplitInput(Ana, 0)]);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.EXPENSE_SPLIT_NO_SHARES, result.ErrorCode);
        }
    }
}
