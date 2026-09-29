namespace Kvit.Domain.MoneyRules
{
    public static class DebtSimplifier
    {
        public static IReadOnlyList<Payment> Simplify(IReadOnlyList<MemberBalance> balancesInJoiningOrder)
        {
            if (balancesInJoiningOrder.Count == 0)
            {
                return [];
            }

            Currency currency = balancesInJoiningOrder[0].Balance.Currency;
            MemberBalance? otherCurrency = balancesInJoiningOrder.FirstOrDefault(balance => balance.Balance.Currency != currency);
            if (otherCurrency is not null)
            {
                throw new InvalidOperationException(
                    $"Debts are simplified one currency at a time, but member {otherCurrency.MemberId}'s balance is in {otherCurrency.Balance.Currency} while the first is in {currency}.");
            }

            long[] remaining = [.. balancesInJoiningOrder.Select(balance => balance.Balance.MinorUnits)];
            Int128 sum = 0;
            foreach (long balance in remaining)
            {
                sum += balance;
            }

            if (sum != 0)
            {
                throw new InvalidOperationException($"Balances in {currency} add up to {sum} minor units instead of 0, so they can't be settled.");
            }

            List<Payment> payments = [];
            while (true)
            {
                int creditorIndex = IndexOfLargest(remaining);
                int debtorIndex = IndexOfSmallest(remaining);
                if (remaining[creditorIndex] == 0)
                {
                    return payments;
                }

                long amount = Math.Min(remaining[creditorIndex], checked(-remaining[debtorIndex]));
                payments.Add(new Payment(
                    balancesInJoiningOrder[debtorIndex].MemberId,
                    balancesInJoiningOrder[creditorIndex].MemberId,
                    Money.Create(amount, currency).Value));
                remaining[creditorIndex] -= amount;
                remaining[debtorIndex] += amount;
            }
        }

        private static int IndexOfLargest(long[] values)
        {
            int largestIndex = 0;
            for (int index = 1; index < values.Length; index++)
            {
                if (values[index] > values[largestIndex])
                {
                    largestIndex = index;
                }
            }

            return largestIndex;
        }

        private static int IndexOfSmallest(long[] values)
        {
            int smallestIndex = 0;
            for (int index = 1; index < values.Length; index++)
            {
                if (values[index] < values[smallestIndex])
                {
                    smallestIndex = index;
                }
            }

            return smallestIndex;
        }
    }
}
