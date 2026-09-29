using System.Globalization;
using Kvit.Domain.Results;

namespace Kvit.Domain.MoneyRules
{
    public static class Splitter
    {
        private const long BasisPointsInWhole = 10000;
        private const int BasisPointsPerPercent = 100;
        private const int FirstInJoiningOrder = 0;

        public static Result<IReadOnlyList<SplitShare>> Calculate(SplitType splitType, Money total, Guid payerMemberId, IReadOnlyList<SplitInput> inputsInJoiningOrder)
        {
            Result validation = ValidateCommon(total, inputsInJoiningOrder);
            if (!validation.IsSuccess)
            {
                return Fail(validation.Error, validation.ErrorCode);
            }

            int leftoverIndex = LeftoverIndex(payerMemberId, inputsInJoiningOrder);
            return splitType switch
            {
                SplitType.Equal => SplitEqually(total, inputsInJoiningOrder, leftoverIndex),
                SplitType.Exact => SplitExactly(total, inputsInJoiningOrder),
                SplitType.Percentage => SplitByPercentage(total, inputsInJoiningOrder, leftoverIndex),
                SplitType.Shares => SplitByShares(total, inputsInJoiningOrder, leftoverIndex),
                _ => throw new ArgumentOutOfRangeException(nameof(splitType), splitType, $"Unknown split type: {splitType}."),
            };
        }

        private static Result ValidateCommon(Money total, IReadOnlyList<SplitInput> inputs)
        {
            if (total.MinorUnits <= 0)
            {
                return Result.Failure(
                    $"Expense amount must be more than 0; got {CurrencyRules.FormatAmount(total.MinorUnits, total.Currency)} {total.Currency}.",
                    ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE);
            }

            if (inputs.Count == 0)
            {
                return Result.Failure("Split has no people in it; at least 1 is needed.", ResultCodes.EXPENSE_SPLIT_NO_PARTICIPANTS);
            }

            HashSet<Guid> seenMembers = [];
            foreach (SplitInput input in inputs)
            {
                if (!seenMembers.Add(input.MemberId))
                {
                    return Result.Failure($"Member {input.MemberId} is listed more than once in the split.", ResultCodes.EXPENSE_SPLIT_DUPLICATE_MEMBER);
                }

                if (input.InputValue < 0)
                {
                    return Result.Failure($"Member {input.MemberId} has a negative split value: {input.InputValue}.", ResultCodes.EXPENSE_SPLIT_NEGATIVE_INPUT);
                }
            }

            return Result.Ok();
        }

        private static int LeftoverIndex(Guid payerMemberId, IReadOnlyList<SplitInput> inputs)
        {
            for (int index = 0; index < inputs.Count; index++)
            {
                if (inputs[index].MemberId == payerMemberId)
                {
                    return index;
                }
            }

            return FirstInJoiningOrder;
        }

        private static Result<IReadOnlyList<SplitShare>> SplitEqually(Money total, IReadOnlyList<SplitInput> inputs, int leftoverIndex)
        {
            Result stepCheck = CheckInputsOnStep(inputs, total.Currency, "extra");
            if (!stepCheck.IsSuccess)
            {
                return Fail(stepCheck.Error, stepCheck.ErrorCode);
            }

            Int128 extrasTotal = SumOfInputs(inputs);
            if (extrasTotal > total.MinorUnits)
            {
                return Fail(
                    $"Extras add up to {CurrencyRules.FormatAmount(extrasTotal, total.Currency)} of {CurrencyRules.FormatAmount(total.MinorUnits, total.Currency)} {total.Currency}; {CurrencyRules.FormatAmount(extrasTotal - total.MinorUnits, total.Currency)} too much.",
                    ResultCodes.EXPENSE_SPLIT_EXTRAS_EXCEED_TOTAL);
            }

            long step = CurrencyRules.StepMinorUnits(total.Currency);
            long rest = total.MinorUnits - (long)extrasTotal;
            long baseShare = rest / (inputs.Count * step) * step;
            long[] roundedShares = [.. inputs.Select(input => baseShare + input.InputValue)];
            return GiveLeftover(total, inputs, roundedShares, leftoverIndex);
        }

        private static Result<IReadOnlyList<SplitShare>> SplitExactly(Money total, IReadOnlyList<SplitInput> inputs)
        {
            Result stepCheck = CheckInputsOnStep(inputs, total.Currency, "amount");
            if (!stepCheck.IsSuccess)
            {
                return Fail(stepCheck.Error, stepCheck.ErrorCode);
            }

            Int128 assigned = SumOfInputs(inputs);
            if (assigned != total.MinorUnits)
            {
                Int128 difference = total.MinorUnits - assigned;
                string differenceText = difference > 0 ? "left to assign" : "over the total";
                return Fail(
                    $"Split adds up to {CurrencyRules.FormatAmount(assigned, total.Currency)} of {CurrencyRules.FormatAmount(total.MinorUnits, total.Currency)} {total.Currency}; {CurrencyRules.FormatAmount(Int128.Abs(difference), total.Currency)} {differenceText}.",
                    ResultCodes.EXPENSE_SPLIT_DOES_NOT_ADD_UP);
            }

            long[] amounts = [.. inputs.Select(input => input.InputValue)];
            return BuildShares(total, inputs, amounts);
        }

        private static Result<IReadOnlyList<SplitShare>> SplitByPercentage(Money total, IReadOnlyList<SplitInput> inputs, int leftoverIndex)
        {
            Int128 basisPointsTotal = SumOfInputs(inputs);
            if (basisPointsTotal != BasisPointsInWhole)
            {
                Int128 difference = BasisPointsInWhole - basisPointsTotal;
                string differenceText = difference > 0 ? "left to assign" : "too much";
                return Fail(
                    $"Percentages add up to {FormatPercent(basisPointsTotal)} %, not 100 %; {FormatPercent(Int128.Abs(difference))} % {differenceText}.",
                    ResultCodes.EXPENSE_SPLIT_DOES_NOT_ADD_UP);
            }

            return SplitProportionally(total, inputs, basisPointsTotal, leftoverIndex);
        }

        private static Result<IReadOnlyList<SplitShare>> SplitByShares(Money total, IReadOnlyList<SplitInput> inputs, int leftoverIndex)
        {
            Int128 sharesTotal = SumOfInputs(inputs);
            if (sharesTotal == 0)
            {
                return Fail($"All {inputs.Count} people in the split have 0 shares; at least 1 share is needed.", ResultCodes.EXPENSE_SPLIT_NO_SHARES);
            }

            return SplitProportionally(total, inputs, sharesTotal, leftoverIndex);
        }

        private static Result<IReadOnlyList<SplitShare>> SplitProportionally(Money total, IReadOnlyList<SplitInput> inputs, Int128 weightTotal, int leftoverIndex)
        {
            long step = CurrencyRules.StepMinorUnits(total.Currency);
            Int128 divisor = weightTotal * step;
            long[] roundedShares = [.. inputs.Select(input => (long)((Int128)total.MinorUnits * input.InputValue / divisor) * step)];
            return GiveLeftover(total, inputs, roundedShares, leftoverIndex);
        }

        private static Result CheckInputsOnStep(IReadOnlyList<SplitInput> inputs, Currency currency, string valueName)
        {
            foreach (SplitInput input in inputs)
            {
                Result<Money> amount = Money.Create(input.InputValue, currency);
                if (!amount.IsSuccess)
                {
                    return Result.Failure($"The {valueName} for member {input.MemberId}: {amount.Error}", amount.ErrorCode);
                }
            }

            return Result.Ok();
        }

        private static Int128 SumOfInputs(IReadOnlyList<SplitInput> inputs)
        {
            Int128 sum = 0;
            foreach (SplitInput input in inputs)
            {
                sum += input.InputValue;
            }

            return sum;
        }

        private static Result<IReadOnlyList<SplitShare>> GiveLeftover(Money total, IReadOnlyList<SplitInput> inputs, long[] roundedShares, int leftoverIndex)
        {
            long leftover = total.MinorUnits - roundedShares.Sum();
            roundedShares[leftoverIndex] += leftover;
            return BuildShares(total, inputs, roundedShares);
        }

        private static Result<IReadOnlyList<SplitShare>> BuildShares(Money total, IReadOnlyList<SplitInput> inputs, long[] amounts)
        {
            List<SplitShare> shares = [.. inputs.Select((input, index) => new SplitShare(input.MemberId, input.InputValue, Money.Create(amounts[index], total.Currency).Value))];
            return Result.Ok<IReadOnlyList<SplitShare>>(shares);
        }

        private static string FormatPercent(Int128 basisPoints)
        {
            string whole = (basisPoints / BasisPointsPerPercent).ToString(CultureInfo.InvariantCulture);
            int fraction = (int)(basisPoints % BasisPointsPerPercent);
            return $"{whole}.{fraction.ToString("00", CultureInfo.InvariantCulture)}";
        }

        private static Result<IReadOnlyList<SplitShare>> Fail(string error, string errorCode) =>
            Result.Failure<IReadOnlyList<SplitShare>>(error, errorCode);
    }
}
