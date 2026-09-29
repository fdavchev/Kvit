using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Xunit;
using static Kvit.Domain.Tests.MoneyRules.MoneyTestData;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class EqualSplitTests
    {
        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void HotelWithMarkoExtra_TakesExtrasOffFirstThenSplitsTheRest(Currency currency)
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Equal,
                Steps(3000, currency),
                Filip,
                new SplitInput(Filip, 0),
                new SplitInput(Ana, 0),
                new SplitInput(Marko, 600 * Step(currency)),
                new SplitInput(Bojan, 0),
                new SplitInput(Elena, 0));

            Assert.Equal([480, 480, 1080, 480, 480], StepsOf(shares, currency));
        }

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void ThousandAmongThree_PayerTakesTheLeftover(Currency currency)
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Equal,
                Steps(1000, currency),
                Marko,
                new SplitInput(Filip, 0),
                new SplitInput(Ana, 0),
                new SplitInput(Marko, 0));

            Assert.Equal([333, 333, 334], StepsOf(shares, currency));
        }

        [Fact]
        public void ThousandDenarsAmongSeven_PayerTakesTheWholeLeftoverOfSix()
        {
            Guid sixth = new("00000000-0000-0000-0000-000000000006");
            Guid seventh = new("00000000-0000-0000-0000-000000000007");

            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Equal,
                Steps(1000, Currency.MKD),
                Filip,
                new SplitInput(Filip, 0),
                new SplitInput(Ana, 0),
                new SplitInput(Marko, 0),
                new SplitInput(Bojan, 0),
                new SplitInput(Elena, 0),
                new SplitInput(sixth, 0),
                new SplitInput(seventh, 0));

            Assert.Equal([148, 142, 142, 142, 142, 142, 142], StepsOf(shares, Currency.MKD));
        }

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void TaxiWhenPayerIsNotSharing_FirstListedTakesTheLeftover(Currency currency)
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Equal,
                Steps(1000, currency),
                Filip,
                new SplitInput(Ana, 0),
                new SplitInput(Marko, 0),
                new SplitInput(Bojan, 0));

            Assert.Equal([334, 333, 333], StepsOf(shares, currency));
        }

        [Fact]
        public void TenEurosAmongThree_PayerGetsThreeThirtyFour()
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Equal,
                Amount(1000, Currency.EUR),
                Filip,
                new SplitInput(Filip, 0),
                new SplitInput(Ana, 0),
                new SplitInput(Marko, 0));

            Assert.Equal([334, 333, 333], ShareAmounts(shares));
        }

        [Fact]
        public void Shares_KeepMembersAndInputValuesInOrder()
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Equal,
                Steps(3000, Currency.MKD),
                Filip,
                new SplitInput(Ana, 0),
                new SplitInput(Marko, 60000));

            Assert.Equal([Ana, Marko], shares.Select(share => share.MemberId));
            Assert.Equal([0, 60000], shares.Select(share => share.InputValue));
            Assert.All(shares, share => Assert.Equal(Currency.MKD, share.Share.Currency));
        }

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void ExtrasMoreThanTheTotal_FailsNamingTheNumbers(Currency currency)
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(
                SplitType.Equal,
                Steps(1000, currency),
                Filip,
                [new SplitInput(Filip, 0), new SplitInput(Marko, 1200 * Step(currency))]);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.EXPENSE_SPLIT_EXTRAS_EXCEED_TOTAL, result.ErrorCode);
            Assert.Contains(CurrencyRules.FormatAmount(1200 * Step(currency), currency), result.Error);
            Assert.Contains(CurrencyRules.FormatAmount(200 * Step(currency), currency), result.Error);
        }

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void ExtrasEqualToTheTotal_GivesEveryoneElseZero(Currency currency)
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Equal,
                Steps(1000, currency),
                Filip,
                new SplitInput(Filip, 0),
                new SplitInput(Ana, 0),
                new SplitInput(Marko, 1000 * Step(currency)));

            Assert.Equal([0, 0, 1000], StepsOf(shares, currency));
        }

        [Fact]
        public void MkdExtraNotOnWholeDenar_FailsWithNotOnStep()
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(
                SplitType.Equal,
                Steps(1000, Currency.MKD),
                Filip,
                [new SplitInput(Filip, 0), new SplitInput(Marko, 5050)]);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.MONEY_NOT_ON_CURRENCY_STEP, result.ErrorCode);
            Assert.Contains(Marko.ToString(), result.Error);
        }

        [Fact]
        public void EurExtraInCents_IsAllowed()
        {
            IReadOnlyList<SplitShare> shares = Split(
                SplitType.Equal,
                Amount(1000, Currency.EUR),
                Filip,
                new SplitInput(Filip, 0),
                new SplitInput(Marko, 5));

            Assert.Equal([498, 502], ShareAmounts(shares));
        }
    }
}
