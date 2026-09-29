using Kvit.Domain.MoneyRules;
using Xunit;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class CurrencyRulesTests
    {
        [Theory]
        [InlineData(Currency.MKD, 100L)]
        [InlineData(Currency.EUR, 1L)]
        public void StepMinorUnits_ReturnsTheCurrencysRoundingStep(Currency currency, long expectedStep)
        {
            long step = CurrencyRules.StepMinorUnits(currency);

            Assert.Equal(expectedStep, step);
        }

        [Fact]
        public void StepMinorUnits_UnknownCurrency_ThrowsNamingTheValue()
        {
            Currency unknown = (Currency)99;

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => CurrencyRules.StepMinorUnits(unknown));

            Assert.Contains("99", exception.Message);
        }

        [Theory]
        [InlineData(300000L, Currency.MKD, "3,000")]
        [InlineData(12050L, Currency.MKD, "120.50")]
        [InlineData(0L, Currency.MKD, "0")]
        [InlineData(-150000L, Currency.MKD, "-1,500")]
        [InlineData(1000L, Currency.EUR, "10.00")]
        [InlineData(123450L, Currency.EUR, "1,234.50")]
        [InlineData(-5L, Currency.EUR, "-0.05")]
        public void FormatAmount_WritesWholeDenarsAndCents(long minorUnits, Currency currency, string expected)
        {
            string text = CurrencyRules.FormatAmount(minorUnits, currency);

            Assert.Equal(expected, text);
        }
    }
}
