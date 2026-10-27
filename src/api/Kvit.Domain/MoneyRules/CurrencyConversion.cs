namespace Kvit.Domain.MoneyRules
{
    public static class CurrencyConversion
    {
        public static long Convert(long minorUnits, Currency from, Currency to, decimal mkdPerEur)
        {
            if (from == to)
            {
                return minorUnits;
            }

            decimal exactMinorUnits = to == Currency.MKD ? minorUnits * mkdPerEur : minorUnits / mkdPerEur;
            long step = CurrencyRules.StepMinorUnits(to);
            decimal steps = Math.Round(exactMinorUnits / step, MidpointRounding.AwayFromZero);

            return (long)(steps * step);
        }
    }
}
