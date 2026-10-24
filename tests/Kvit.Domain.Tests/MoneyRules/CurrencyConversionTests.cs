using Kvit.Domain.MoneyRules;
using Xunit;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class CurrencyConversionTests
    {
        private const decimal SeededRate = 61.5610m;

        public static TheoryData<long, Currency, long> SameCurrencyAmounts => new()
        {
            { 123_456L, Currency.MKD, 123_456L },
            { 123_450L, Currency.EUR, 123_450L },
            { 0L, Currency.MKD, 0L },
            { -9_900L, Currency.EUR, -9_900L },
            { long.MaxValue, Currency.MKD, long.MaxValue },
            { long.MinValue, Currency.EUR, long.MinValue },
        };

        public static TheoryData<long, decimal, long> EurToMkdAmounts => new()
        {
            { 2_000L, SeededRate, 123_100L },
            { 150L, SeededRate, 9_200L },
            { 1_234L, SeededRate, 76_000L },
            { 12_345L, 61.5000m, 759_200L },
            { 100_000_000_000_000_000L, SeededRate, 6_156_100_000_000_000_000L },
        };

        public static TheoryData<long, decimal, long> EurToMkdMidpoints => new()
        {
            { 1L, 50.0000m, 100L },
            { 3L, 50.0000m, 200L },
            { 4L, 62.5000m, 300L },
            { -1L, 50.0000m, -100L },
            { -3L, 50.0000m, -200L },
            { -4L, 62.5000m, -300L },
        };

        public static TheoryData<long, decimal, long> EurToMkdAroundTheMidpoint => new()
        {
            { 1L, 49.9999m, 0L },
            { 1L, 50.0001m, 100L },
            { -1L, 49.9999m, 0L },
            { -1L, 50.0001m, -100L },
        };

        public static TheoryData<long, decimal, long> MkdToEurAmounts => new()
        {
            { 123_100L, SeededRate, 2_000L },
            { 100_000L, SeededRate, 1_624L },
            { 9_200L, SeededRate, 149L },
            { long.MaxValue, SeededRate, 149_824_922_221_126_619L },
        };

        public static TheoryData<long, decimal, long> MkdToEurMidpoints => new()
        {
            { 25L, 50.0000m, 1L },
            { 125L, 50.0000m, 3L },
            { 100L, 40.0000m, 3L },
            { -25L, 50.0000m, -1L },
            { -125L, 50.0000m, -3L },
            { -100L, 40.0000m, -3L },
        };

        public static TheoryData<long, decimal, long> MkdToEurAroundTheMidpoint => new()
        {
            { 24L, 50.0000m, 0L },
            { 26L, 50.0000m, 1L },
            { -24L, 50.0000m, 0L },
            { -26L, 50.0000m, -1L },
        };

        [Theory]
        [MemberData(nameof(SameCurrencyAmounts))]
        public void Convert_SameCurrency_ReturnsTheAmountUntouched(long minorUnits, Currency currency, long expected)
        {
            long converted = CurrencyConversion.Convert(minorUnits, currency, currency, SeededRate);

            Assert.Equal(expected, converted);
        }

        [Fact]
        public void Convert_TwentyEuroAtTheSeededRate_IsExactly1231Denars()
        {
            long converted = CurrencyConversion.Convert(2_000L, Currency.EUR, Currency.MKD, SeededRate);

            Assert.Equal(123_100L, converted);
        }

        [Fact]
        public void Convert_1231DenarsAtTheSeededRate_IsTwentyEuroBecause19Point9964RoundsToTheCent()
        {
            long converted = CurrencyConversion.Convert(123_100L, Currency.MKD, Currency.EUR, SeededRate);

            Assert.Equal(2_000L, converted);
        }

        [Theory]
        [MemberData(nameof(EurToMkdAmounts))]
        public void Convert_EurToMkd_RoundsToTheWholeDenar(long cents, decimal mkdPerEur, long expectedDeni)
        {
            long converted = CurrencyConversion.Convert(cents, Currency.EUR, Currency.MKD, mkdPerEur);

            Assert.Equal(expectedDeni, converted);
            Assert.Equal(0, converted % 100);
        }

        [Theory]
        [MemberData(nameof(EurToMkdMidpoints))]
        public void Convert_EurToMkdExactlyHalfADenar_RoundsAwayFromZero(long cents, decimal mkdPerEur, long expectedDeni)
        {
            long converted = CurrencyConversion.Convert(cents, Currency.EUR, Currency.MKD, mkdPerEur);

            Assert.Equal(expectedDeni, converted);
        }

        [Theory]
        [MemberData(nameof(EurToMkdAroundTheMidpoint))]
        public void Convert_EurToMkdJustBelowOrAboveHalfADenar_RoundsToTheNearestDenar(long cents, decimal mkdPerEur, long expectedDeni)
        {
            long converted = CurrencyConversion.Convert(cents, Currency.EUR, Currency.MKD, mkdPerEur);

            Assert.Equal(expectedDeni, converted);
        }

        [Theory]
        [MemberData(nameof(MkdToEurAmounts))]
        public void Convert_MkdToEur_RoundsToTheCent(long deni, decimal mkdPerEur, long expectedCents)
        {
            long converted = CurrencyConversion.Convert(deni, Currency.MKD, Currency.EUR, mkdPerEur);

            Assert.Equal(expectedCents, converted);
        }

        [Theory]
        [MemberData(nameof(MkdToEurMidpoints))]
        public void Convert_MkdToEurExactlyHalfACent_RoundsAwayFromZero(long deni, decimal mkdPerEur, long expectedCents)
        {
            long converted = CurrencyConversion.Convert(deni, Currency.MKD, Currency.EUR, mkdPerEur);

            Assert.Equal(expectedCents, converted);
        }

        [Theory]
        [MemberData(nameof(MkdToEurAroundTheMidpoint))]
        public void Convert_MkdToEurJustBelowOrAboveHalfACent_RoundsToTheNearestCent(long deni, decimal mkdPerEur, long expectedCents)
        {
            long converted = CurrencyConversion.Convert(deni, Currency.MKD, Currency.EUR, mkdPerEur);

            Assert.Equal(expectedCents, converted);
        }

        [Theory]
        [InlineData(Currency.EUR, Currency.MKD)]
        [InlineData(Currency.MKD, Currency.EUR)]
        public void Convert_Zero_IsZeroInBothDirections(Currency from, Currency to)
        {
            long converted = CurrencyConversion.Convert(0L, from, to, SeededRate);

            Assert.Equal(0L, converted);
        }

        [Fact]
        public void Convert_NegativeEurToMkd_MirrorsThePositiveAmount()
        {
            long converted = CurrencyConversion.Convert(-2_000L, Currency.EUR, Currency.MKD, SeededRate);

            Assert.Equal(-123_100L, converted);
        }

        [Fact]
        public void Convert_NegativeMkdToEur_MirrorsThePositiveAmount()
        {
            long converted = CurrencyConversion.Convert(-123_100L, Currency.MKD, Currency.EUR, SeededRate);

            Assert.Equal(-2_000L, converted);
        }

        [Fact]
        public void Convert_LargestWholeDenarAmountAtRateOne_StaysInsideTheLongRange()
        {
            long fromMaximum = CurrencyConversion.Convert(long.MaxValue, Currency.EUR, Currency.MKD, 1.0000m);
            long fromMinimum = CurrencyConversion.Convert(long.MinValue, Currency.EUR, Currency.MKD, 1.0000m);

            Assert.Equal(9_223_372_036_854_775_800L, fromMaximum);
            Assert.Equal(-9_223_372_036_854_775_800L, fromMinimum);
        }

        [Fact]
        public void Convert_LargestMinorAmountMkdToEurAtRateOne_ReturnsItUnchanged()
        {
            long fromMinimum = CurrencyConversion.Convert(long.MinValue, Currency.MKD, Currency.EUR, 1.0000m);
            long fromMaximum = CurrencyConversion.Convert(long.MaxValue, Currency.MKD, Currency.EUR, 1.0000m);

            Assert.Equal(long.MinValue, fromMinimum);
            Assert.Equal(long.MaxValue, fromMaximum);
        }

        [Theory]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        [InlineData(200_000_000_000_000_000L)]
        public void Convert_EurToMkdWhoseResultDoesNotFitALong_ThrowsInsteadOfWrapping(long cents)
        {
            Assert.ThrowsAny<OverflowException>(() => CurrencyConversion.Convert(cents, Currency.EUR, Currency.MKD, SeededRate));
        }

        [Theory]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        public void Convert_MkdToEurWhoseResultDoesNotFitALong_ThrowsInsteadOfWrapping(long deni)
        {
            Assert.ThrowsAny<OverflowException>(() => CurrencyConversion.Convert(deni, Currency.MKD, Currency.EUR, 0.0001m));
        }
    }
}
