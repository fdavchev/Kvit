using System.Globalization;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class MoneyTests
    {
        [Theory]
        [InlineData(300000L, Currency.MKD)]
        [InlineData(0L, Currency.MKD)]
        [InlineData(1550L, Currency.EUR)]
        [InlineData(12050L, Currency.EUR)]
        public void Create_OnTheCurrencyStep_Succeeds(long minorUnits, Currency currency)
        {
            Result<Money> result = Money.Create(minorUnits, currency);

            Assert.True(result.IsSuccess);
            Assert.Equal(minorUnits, result.Value.MinorUnits);
            Assert.Equal(currency, result.Value.Currency);
        }

        [Theory]
        [InlineData(12050L)]
        [InlineData(1L)]
        [InlineData(-150L)]
        public void Create_MkdNotOnWholeDenar_FailsWithNotOnStep(long minorUnits)
        {
            Result<Money> result = Money.Create(minorUnits, Currency.MKD);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.MONEY_NOT_ON_CURRENCY_STEP, result.ErrorCode);
            Assert.Contains(minorUnits.ToString(CultureInfo.InvariantCulture), result.Error);
        }

        [Theory]
        [InlineData(-150000L, Currency.MKD)]
        [InlineData(-5L, Currency.EUR)]
        public void Create_NegativeOnStep_IsAllowedForBalances(long minorUnits, Currency currency)
        {
            Result<Money> result = Money.Create(minorUnits, currency);

            Assert.True(result.IsSuccess);
            Assert.Equal(minorUnits, result.Value.MinorUnits);
        }

        [Fact]
        public void Create_UnknownCurrency_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Money.Create(100, (Currency)99));
        }

        [Theory]
        [InlineData(Currency.MKD)]
        [InlineData(Currency.EUR)]
        public void Zero_IsZeroInThatCurrency(Currency currency)
        {
            Money zero = Money.Zero(currency);

            Assert.Equal(0, zero.MinorUnits);
            Assert.Equal(currency, zero.Currency);
        }

        [Theory]
        [InlineData(Currency.MKD, 120000L, 30000L, 150000L)]
        [InlineData(Currency.EUR, 1550L, 1L, 1551L)]
        [InlineData(Currency.EUR, 100L, -250L, -150L)]
        public void Add_SameCurrency_ReturnsTheSum(Currency currency, long left, long right, long expected)
        {
            Money first = MoneyTestData.Amount(left, currency);
            Money second = MoneyTestData.Amount(right, currency);

            Result<Money> result = first.Add(second);

            Assert.True(result.IsSuccess);
            Assert.Equal(MoneyTestData.Amount(expected, currency), result.Value);
        }

        [Theory]
        [InlineData(Currency.MKD, 120000L, 30000L, 90000L)]
        [InlineData(Currency.MKD, 30000L, 120000L, -90000L)]
        [InlineData(Currency.EUR, 1550L, 1L, 1549L)]
        public void Subtract_SameCurrency_ReturnsTheDifference(Currency currency, long left, long right, long expected)
        {
            Money first = MoneyTestData.Amount(left, currency);
            Money second = MoneyTestData.Amount(right, currency);

            Result<Money> result = first.Subtract(second);

            Assert.True(result.IsSuccess);
            Assert.Equal(MoneyTestData.Amount(expected, currency), result.Value);
        }

        [Fact]
        public void Add_DifferentCurrencies_FailsWithMismatch()
        {
            Money denars = MoneyTestData.Amount(120000, Currency.MKD);
            Money euros = MoneyTestData.Amount(1550, Currency.EUR);

            Result<Money> result = denars.Add(euros);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.MONEY_CURRENCY_MISMATCH, result.ErrorCode);
            Assert.Contains("1,200 MKD", result.Error);
            Assert.Contains("15.50 EUR", result.Error);
        }

        [Fact]
        public void Subtract_DifferentCurrencies_FailsWithMismatch()
        {
            Money euros = MoneyTestData.Amount(1550, Currency.EUR);
            Money denars = MoneyTestData.Amount(120000, Currency.MKD);

            Result<Money> result = euros.Subtract(denars);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.MONEY_CURRENCY_MISMATCH, result.ErrorCode);
        }

        [Fact]
        public void Add_BeyondTheLargestAmount_ThrowsOverflow()
        {
            Money largest = MoneyTestData.Amount(long.MaxValue, Currency.EUR);
            Money oneCent = MoneyTestData.Amount(1, Currency.EUR);

            Assert.Throws<OverflowException>(() => largest.Add(oneCent));
        }

        [Fact]
        public void Equality_SameAmountAndCurrency_AreEqual()
        {
            Money first = MoneyTestData.Amount(1000, Currency.EUR);
            Money second = MoneyTestData.Amount(1000, Currency.EUR);
            Money denars = MoneyTestData.Amount(1000 * 100, Currency.MKD);

            Assert.Equal(first, second);
            Assert.NotEqual(first, denars);
        }
    }
}
