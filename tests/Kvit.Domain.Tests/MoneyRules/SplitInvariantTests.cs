using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class SplitInvariantTests
    {
        private const int Seed = 20260929;
        private const int CaseCount = 2000;
        private const long BasisPointsInWhole = 10000;

        [Theory]
        [InlineData(SplitType.Equal, Currency.MKD)]
        [InlineData(SplitType.Equal, Currency.EUR)]
        [InlineData(SplitType.Exact, Currency.MKD)]
        [InlineData(SplitType.Exact, Currency.EUR)]
        [InlineData(SplitType.Percentage, Currency.MKD)]
        [InlineData(SplitType.Percentage, Currency.EUR)]
        [InlineData(SplitType.Shares, Currency.MKD)]
        [InlineData(SplitType.Shares, Currency.EUR)]
        public void RandomValidSplits_AlwaysAddUpAndOnlyTheLeftoverPersonGetsTheLeftover(SplitType splitType, Currency currency)
        {
            Random random = new(Seed);
            long step = CurrencyRules.StepMinorUnits(currency);

            for (int caseNumber = 0; caseNumber < CaseCount; caseNumber++)
            {
                int memberCount = random.Next(1, 9);
                Guid[] members = [.. Enumerable.Range(0, memberCount).Select(_ => MoneyTestData.RandomMemberId(random))];
                long total = random.NextInt64(1, 1_000_000) * step;
                bool isPayerInSplit = random.Next(4) != 0;
                Guid payer = isPayerInSplit ? members[random.Next(memberCount)] : MoneyTestData.RandomMemberId(random);
                int leftoverIndex = isPayerInSplit ? Array.IndexOf(members, payer) : 0;
                long[] inputValues = RandomInputs(random, splitType, memberCount, total, step);
                SplitInput[] inputs = [.. members.Select((memberId, index) => new SplitInput(memberId, inputValues[index]))];

                Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(splitType, MoneyTestData.Amount(total, currency), payer, inputs);

                Assert.True(result.IsSuccess, $"Case {caseNumber}: {result.ErrorCode} {result.Error}");
                IReadOnlyList<SplitShare> shares = result.Value;
                Assert.Equal(members, shares.Select(share => share.MemberId));
                Assert.Equal(total, shares.Sum(share => share.Share.MinorUnits));
                Assert.All(shares, share =>
                {
                    Assert.True(share.Share.MinorUnits >= 0);
                    Assert.Equal(0, share.Share.MinorUnits % step);
                    Assert.Equal(currency, share.Share.Currency);
                });

                long[] amounts = MoneyTestData.ShareAmounts(shares);
                string caseLabel = $"Case {caseNumber}: total {total}, inputs [{string.Join(", ", inputValues)}], shares [{string.Join(", ", amounts)}], leftover index {leftoverIndex}";
                AssertOnlyLeftoverPersonIsAboveRoundedDown(splitType, total, step, inputValues, amounts, leftoverIndex, caseLabel);
            }
        }

        private static long[] RandomInputs(Random random, SplitType splitType, int memberCount, long total, long step) => splitType switch
        {
            SplitType.Equal => RandomExtras(random, memberCount, total, step),
            SplitType.Exact => [.. RandomParts(random, memberCount, total / step).Select(steps => steps * step)],
            SplitType.Percentage => RandomParts(random, memberCount, BasisPointsInWhole),
            SplitType.Shares => RandomShares(random, memberCount),
            _ => throw new ArgumentOutOfRangeException(nameof(splitType), splitType, $"Unknown split type: {splitType}."),
        };

        private static long[] RandomExtras(Random random, int memberCount, long total, long step)
        {
            long maxStepsPerPerson = total / step / memberCount;
            return [.. Enumerable.Range(0, memberCount).Select(_ => random.Next(3) == 0 ? random.NextInt64(0, maxStepsPerPerson + 1) * step : 0)];
        }

        private static long[] RandomParts(Random random, int memberCount, long whole)
        {
            long[] cuts = [.. Enumerable.Range(0, memberCount - 1).Select(_ => random.NextInt64(0, whole + 1)).Order()];
            long[] edges = [0, .. cuts, whole];
            return [.. Enumerable.Range(0, memberCount).Select(index => edges[index + 1] - edges[index])];
        }

        private static long[] RandomShares(Random random, int memberCount)
        {
            long[] shares = [.. Enumerable.Range(0, memberCount).Select(_ => (long)random.Next(0, 6))];
            if (shares.All(share => share == 0))
            {
                shares[random.Next(memberCount)] = 1;
            }

            return shares;
        }

        private static void AssertOnlyLeftoverPersonIsAboveRoundedDown(SplitType splitType, long total, long step, long[] inputValues, long[] amounts, int leftoverIndex, string caseLabel)
        {
            switch (splitType)
            {
                case SplitType.Exact:
                    Assert.Equal(inputValues, amounts);
                    break;
                case SplitType.Equal:
                    long rest = total - inputValues.Sum();
                    long[] equalParts = [.. amounts.Select((amount, index) => amount - inputValues[index])];
                    Assert.True(equalParts.Where((_, index) => index != leftoverIndex).Distinct().Count() <= 1, $"{caseLabel}: the equal parts differ.");
                    AssertRoundedDownExceptLeftover(equalParts, leftoverIndex, step, (_, equalPart) => IsEqualPartRoundedDown(equalPart, rest, inputValues.Length, step), caseLabel);
                    break;
                case SplitType.Percentage:
                case SplitType.Shares:
                    Int128 weightTotal = inputValues.Sum();
                    AssertRoundedDownExceptLeftover(amounts, leftoverIndex, step, (index, amount) => IsProportionalPartRoundedDown(amount, total, inputValues[index], weightTotal, step), caseLabel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(splitType), splitType, $"Unknown split type: {splitType}.");
            }
        }

        private static void AssertRoundedDownExceptLeftover(long[] parts, int leftoverIndex, long step, Func<int, long, bool> isRoundedDown, string caseLabel)
        {
            for (int index = 0; index < parts.Length; index++)
            {
                long part = parts[index];
                if (index != leftoverIndex)
                {
                    Assert.True(isRoundedDown(index, part), $"{caseLabel}: person {index} is not rounded down to the step.");
                    continue;
                }

                bool isWithinStepsAboveRoundedDown = Enumerable.Range(0, parts.Length).Any(stepsAbove => isRoundedDown(index, part - (stepsAbove * step)));
                Assert.True(isWithinStepsAboveRoundedDown, $"{caseLabel}: the leftover person is not 0 to {parts.Length - 1} steps above their rounded-down part.");
            }
        }

        private static bool IsEqualPartRoundedDown(long equalPart, long rest, int count, long step) =>
            equalPart % step == 0
            && (Int128)equalPart * count <= rest
            && ((Int128)equalPart + step) * count > rest;

        private static bool IsProportionalPartRoundedDown(long amount, long total, long weight, Int128 weightTotal, long step)
        {
            Int128 exactTimesWeightTotal = (Int128)total * weight;
            return amount % step == 0
                && (Int128)amount * weightTotal <= exactTimesWeightTotal
                && ((Int128)amount + step) * weightTotal > exactTimesWeightTotal;
        }
    }
}
