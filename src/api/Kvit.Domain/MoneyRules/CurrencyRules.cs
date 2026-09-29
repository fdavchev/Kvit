using System.Globalization;

namespace Kvit.Domain.MoneyRules
{
    public static class CurrencyRules
    {
        private const int MinorUnitsPerMajorUnit = 100;

        public static long StepMinorUnits(Currency currency) => currency switch
        {
            Currency.MKD => 100,
            Currency.EUR => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, $"Unknown currency: {currency}."),
        };

        public static string FormatAmount(Int128 minorUnits, Currency currency)
        {
            string sign = minorUnits < 0 ? "-" : string.Empty;
            Int128 absolute = Int128.Abs(minorUnits);
            string whole = (absolute / MinorUnitsPerMajorUnit).ToString("N0", CultureInfo.InvariantCulture);
            int fraction = (int)(absolute % MinorUnitsPerMajorUnit);
            bool isWholeUnitCurrency = StepMinorUnits(currency) == MinorUnitsPerMajorUnit;
            if (isWholeUnitCurrency && fraction == 0)
            {
                return $"{sign}{whole}";
            }

            return $"{sign}{whole}.{fraction.ToString("00", CultureInfo.InvariantCulture)}";
        }
    }
}
