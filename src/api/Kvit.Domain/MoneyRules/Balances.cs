namespace Kvit.Domain.MoneyRules
{
    public static class Balances
    {
        public static BalanceSummary Calculate(
            IReadOnlyList<Guid> membersInJoiningOrder,
            IReadOnlyList<BalanceExpense> expenses,
            IReadOnlyList<BalanceSettlement> settlements)
        {
            Dictionary<Guid, int> memberIndexes = IndexMembers(membersInJoiningOrder);
            SortedDictionary<Currency, long[]> totals = [];

            foreach (BalanceExpense expense in expenses.Where(expense => !expense.IsDeleted))
            {
                CheckExpense(expense);
                long[] currencyTotals = TotalsFor(totals, expense.Amount.Currency, membersInJoiningOrder.Count);
                Change(currencyTotals, memberIndexes, expense.PayerMemberId, expense.Amount.MinorUnits);
                foreach (SplitShare share in expense.Shares)
                {
                    Change(currencyTotals, memberIndexes, share.MemberId, -share.Share.MinorUnits);
                }
            }

            bool isAnySettlementPending = false;
            foreach (BalanceSettlement settlement in settlements.Where(settlement => !settlement.IsDeleted && settlement.Status is SettlementStatus.Confirmed or SettlementStatus.Pending))
            {
                CheckSettlement(settlement);
                if (settlement.Status == SettlementStatus.Pending)
                {
                    isAnySettlementPending = true;
                    continue;
                }

                long[] currencyTotals = TotalsFor(totals, settlement.Amount.Currency, membersInJoiningOrder.Count);
                Change(currencyTotals, memberIndexes, settlement.FromMemberId, settlement.Amount.MinorUnits);
                Change(currencyTotals, memberIndexes, settlement.ToMemberId, -settlement.Amount.MinorUnits);
            }

            Dictionary<Currency, IReadOnlyList<MemberBalance>> balances = [];
            foreach (KeyValuePair<Currency, long[]> currencyTotals in totals)
            {
                balances[currencyTotals.Key] = [.. membersInJoiningOrder.Select((memberId, index) =>
                    new MemberBalance(memberId, Money.Create(currencyTotals.Value[index], currencyTotals.Key).Value))];
            }

            bool isEveryBalanceZero = totals.Values.All(currencyTotals => currencyTotals.All(total => total == 0));
            return new BalanceSummary(balances, isEveryBalanceZero && !isAnySettlementPending);
        }

        private static Dictionary<Guid, int> IndexMembers(IReadOnlyList<Guid> membersInJoiningOrder)
        {
            Dictionary<Guid, int> memberIndexes = [];
            for (int index = 0; index < membersInJoiningOrder.Count; index++)
            {
                if (!memberIndexes.TryAdd(membersInJoiningOrder[index], index))
                {
                    throw new InvalidOperationException($"Member {membersInJoiningOrder[index]} is listed more than once in the group's member list.");
                }
            }

            return memberIndexes;
        }

        private static long[] TotalsFor(SortedDictionary<Currency, long[]> totals, Currency currency, int memberCount)
        {
            if (!totals.TryGetValue(currency, out long[]? currencyTotals))
            {
                currencyTotals = new long[memberCount];
                totals[currency] = currencyTotals;
            }

            return currencyTotals;
        }

        private static void CheckExpense(BalanceExpense expense)
        {
            if (expense.Amount.MinorUnits <= 0)
            {
                throw new InvalidOperationException(
                    $"Expense paid by member {expense.PayerMemberId} is {expense.Amount.MinorUnits} minor units of {expense.Amount.Currency}; an expense must be more than 0.");
            }

            long sharesTotal = 0;
            foreach (SplitShare share in expense.Shares)
            {
                if (share.Share.Currency != expense.Amount.Currency)
                {
                    throw new InvalidOperationException(
                        $"Expense paid by member {expense.PayerMemberId} is in {expense.Amount.Currency}, but member {share.MemberId}'s share is in {share.Share.Currency}.");
                }

                if (share.Share.MinorUnits < 0)
                {
                    throw new InvalidOperationException(
                        $"Expense paid by member {expense.PayerMemberId} gives member {share.MemberId} a share of {share.Share.MinorUnits} minor units of {share.Share.Currency}; a share can't be negative.");
                }

                sharesTotal = checked(sharesTotal + share.Share.MinorUnits);
            }

            if (sharesTotal != expense.Amount.MinorUnits)
            {
                throw new InvalidOperationException(
                    $"Expense paid by member {expense.PayerMemberId} is {expense.Amount.MinorUnits} minor units of {expense.Amount.Currency}, but its shares add up to {sharesTotal}.");
            }
        }

        private static void CheckSettlement(BalanceSettlement settlement)
        {
            if (settlement.Amount.MinorUnits <= 0)
            {
                throw new InvalidOperationException(
                    $"Payment from member {settlement.FromMemberId} to member {settlement.ToMemberId} is {settlement.Amount.MinorUnits} minor units of {settlement.Amount.Currency}; a payment must be more than 0.");
            }

            if (settlement.FromMemberId == settlement.ToMemberId)
            {
                throw new InvalidOperationException(
                    $"Payment of {settlement.Amount.MinorUnits} minor units of {settlement.Amount.Currency} goes from member {settlement.FromMemberId} to the same member.");
            }
        }

        private static void Change(long[] currencyTotals, Dictionary<Guid, int> memberIndexes, Guid memberId, long minorUnits)
        {
            if (!memberIndexes.TryGetValue(memberId, out int index))
            {
                throw new InvalidOperationException($"Member {memberId} appears in an expense or settlement but is not in the group's member list.");
            }

            currencyTotals[index] = checked(currencyTotals[index] + minorUnits);
        }
    }
}
