namespace Kvit.Domain.MoneyRules
{
    public static class PerPersonShare
    {
        public static long Of(Money total, int people)
        {
            if (people <= 0)
            {
                throw new InvalidOperationException($"{total.MinorUnits} {total.Currency} cannot be shared by {people} people; a share needs at least one person.");
            }

            long step = CurrencyRules.StepMinorUnits(total.Currency);
            long totalSteps = total.MinorUnits / step;
            long wholeSteps = Math.DivRem(totalSteps, people, out long leftoverSteps);
            bool isHalfOrMore = Math.Abs(leftoverSteps) * 2 >= people;
            long roundedSteps = isHalfOrMore ? wholeSteps + Math.Sign(totalSteps) : wholeSteps;

            return roundedSteps * step;
        }
    }
}
