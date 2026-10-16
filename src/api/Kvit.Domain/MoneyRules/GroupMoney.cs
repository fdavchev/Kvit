using Kvit.Domain.Results;

namespace Kvit.Domain.MoneyRules
{
    public sealed record GroupMoney(
        IReadOnlyList<Guid> MemberIdsInJoiningOrder,
        IReadOnlyList<BalanceExpense> Expenses,
        IReadOnlyList<BalanceSettlement> Settlements)
    {
        public Result CheckMemberSettled(Guid memberId)
        {
            BalanceSummary summary = Balances.Calculate(MemberIdsInJoiningOrder, Expenses, Settlements);
            bool isBalanceZero = summary.ByCurrency.Values.All(balances => BalanceOf(balances, memberId).MinorUnits == 0);
            if (!isBalanceZero)
            {
                return Result.Failure($"Member {memberId} still owes or is owed money in this group.", ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            }

            if (PendingSettlements().Any(settlement => settlement.FromMemberId == memberId || settlement.ToMemberId == memberId))
            {
                return Result.Failure($"Member {memberId} has a payment in this group that is still waiting for an answer.", ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
            }

            return Result.Ok();
        }

        public Result CheckEveryoneSettled()
        {
            BalanceSummary summary = Balances.Calculate(MemberIdsInJoiningOrder, Expenses, Settlements);
            bool isEveryBalanceZero = summary.ByCurrency.Values.All(balances => balances.All(balance => balance.Balance.MinorUnits == 0));
            if (!isEveryBalanceZero)
            {
                return Result.Failure("Someone in this group still owes or is owed money.", ResultCodes.GROUP_BALANCES_NOT_ZERO);
            }

            if (PendingSettlements().Any())
            {
                return Result.Failure("A payment in this group is still waiting for an answer.", ResultCodes.GROUP_HAS_PENDING_PAYMENT);
            }

            return Result.Ok();
        }

        public Result CheckCanStartClosing()
        {
            if (!HasCountedExpense())
            {
                return Result.Failure("This group has no expense that counts yet.", ResultCodes.GROUP_NO_EXPENSES);
            }

            Result settled = CheckEveryoneSettled();
            if (!settled.IsSuccess)
            {
                return Result.Failure($"Not everyone in this group is kvit: {settled.Error}", ResultCodes.GROUP_NOT_KVIT);
            }

            return Result.Ok();
        }

        public IReadOnlyList<Money> NonZeroBalancesOf(Guid memberId)
        {
            BalanceSummary summary = Balances.Calculate(MemberIdsInJoiningOrder, Expenses, Settlements);

            return [.. summary.ByCurrency
                .OrderBy(currencyBalances => currencyBalances.Key)
                .Select(currencyBalances => BalanceOf(currencyBalances.Value, memberId))
                .Where(balance => balance.MinorUnits != 0)];
        }

        public bool IsPaidOff()
        {
            bool hasConfirmedPayment = Settlements.Any(settlement => !settlement.IsDeleted && settlement.Status == SettlementStatus.Confirmed);

            return HasCountedExpense() && hasConfirmedPayment && CheckEveryoneSettled().IsSuccess;
        }

        private bool HasCountedExpense()
        {
            return Expenses.Any(expense => !expense.IsDeleted);
        }

        private static Money BalanceOf(IReadOnlyList<MemberBalance> balances, Guid memberId)
        {
            MemberBalance balance = balances.SingleOrDefault(candidate => candidate.MemberId == memberId)
                ?? throw new InvalidOperationException($"Member {memberId} is not in the group's member list, so its balance can't be checked.");

            return balance.Balance;
        }

        private IEnumerable<BalanceSettlement> PendingSettlements()
        {
            return Settlements.Where(settlement => !settlement.IsDeleted && settlement.Status == SettlementStatus.Pending);
        }
    }
}
