using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Xunit;
using static Kvit.Domain.Tests.MoneyRules.MoneyTestData;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class PercentageSplitTests
    {
        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void ThirdsOfAThousand_RoundDownAndPayerTakesTheLeftover(Currency currency)
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Percentage,
                Steps(1000, currency),
                Filip,
                new SplitInput(Filip, 3333),
                new SplitInput(Ana, 3333),
                new SplitInput(Marko, 3334));

            Assert.Equal([334, 333, 333], StepsOf(shares, currency));
        }

        [Fact]
        public void TenEurosInThirds_PayerGetsThreeThirtyFour()
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Percentage,
                Amount(1000, Currency.EUR),
                Marko,
                new SplitInput(Filip, 3333),
                new SplitInput(Ana, 3333),
                new SplitInput(Marko, 3334));

            Assert.Equal([333, 333, 334], ShareAmounts(shares));
        }

        [Fact]
        public void PercentagesUnderOneHundred_FailsNamingTheSum()
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(
                SplitType.Percentage,
                Steps(1000, Currency.MKD),
                Filip,
                [new SplitInput(Filip, 3333), new SplitInput(Ana, 3333), new SplitInput(Marko, 3333)]);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.EXPENSE_SPLIT_DOES_NOT_ADD_UP, result.ErrorCode);
            Assert.Equal("Percentages add up to 99.99 %, not 100 %; 0.01 % left to assign.", result.Error);
        }

        [Fact]
        public void PercentagesOverOneHundred_FailsNamingTheSum()
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(
                SplitType.Percentage,
                Amount(1000, Currency.EUR),
                Filip,
                [new SplitInput(Filip, 6000), new SplitInput(Ana, 5000)]);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.EXPENSE_SPLIT_DOES_NOT_ADD_UP, result.ErrorCode);
            Assert.Equal("Percentages add up to 110.00 %, not 100 %; 10.00 % too much.", result.Error);
        }

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void PayerAtZeroPercent_StillTakesTheLeftover(Currency currency)
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Percentage,
                Steps(1000, currency),
                Filip,
                new SplitInput(Filip, 0),
                new SplitInput(Ana, 3333),
                new SplitInput(Marko, 3333),
                new SplitInput(Bojan, 3334));

            Assert.Equal([1, 333, 333, 333], StepsOf(shares, currency));
        }

        [Fact]
        public void PayerNotInTheSplit_FirstListedTakesTheLeftover()
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Percentage,
                Steps(1000, Currency.MKD),
                Outsider,
                new SplitInput(Ana, 3333),
                new SplitInput(Marko, 3333),
                new SplitInput(Bojan, 3334));

            Assert.Equal([334, 333, 333], StepsOf(shares, Currency.MKD));
        }
    }
}
