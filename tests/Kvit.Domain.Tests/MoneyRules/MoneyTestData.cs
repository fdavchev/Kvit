using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Tests.MoneyRules
{
    public static class MoneyTestData
    {
        public static readonly Guid Filip = new("00000000-0000-0000-0000-000000000001");
        public static readonly Guid Ana = new("00000000-0000-0000-0000-000000000002");
        public static readonly Guid Marko = new("00000000-0000-0000-0000-000000000003");
        public static readonly Guid Bojan = new("00000000-0000-0000-0000-000000000004");
        public static readonly Guid Elena = new("00000000-0000-0000-0000-000000000005");
        public static readonly Guid Outsider = new("00000000-0000-0000-0000-000000000099");

        public static long Step(Currency currency) => CurrencyRules.StepMinorUnits(currency);

        public static Guid RandomMemberId(Random random)
        {
            byte[] bytes = new byte[16];
            random.NextBytes(bytes);
            return new Guid(bytes);
        }

        public static Money Amount(long minorUnits, Currency currency) => Money.Create(minorUnits, currency).Value;

        public static Money Steps(long steps, Currency currency) => Amount(steps * Step(currency), currency);

        public static IReadOnlyList<SplitShare> Split(SplitType splitType, Money total, Guid payerMemberId, params SplitInput[] inputs)
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(splitType, total, payerMemberId, inputs);
            return result.Value;
        }

        public static long[] ShareAmounts(IReadOnlyList<SplitShare> shares) => [.. shares.Select(share => share.Share.MinorUnits)];

        public static long[] StepsOf(IReadOnlyList<SplitShare> shares, Currency currency) => [.. shares.Select(share => share.Share.MinorUnits / Step(currency))];
    }
}
