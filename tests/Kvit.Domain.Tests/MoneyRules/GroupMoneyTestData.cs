using Kvit.Domain.MoneyRules;
using static Kvit.Domain.Tests.MoneyRules.MoneyTestData;

namespace Kvit.Domain.Tests.MoneyRules
{
    public static class GroupMoneyTestData
    {
        public static BalanceExpense EqualExpense(Guid payer, Money amount, params Guid[] people)
        {
            IReadOnlyList<SplitShare> shares = Split(SplitType.Equal, amount, payer, [.. people.Select(person => new SplitInput(person, 0))]);

            return new BalanceExpense(payer, amount, shares, IsDeleted: false);
        }

        public static BalanceSettlement Settlement(Guid from, Guid to, Money amount, SettlementStatus status, bool isDeleted = false)
        {
            return new BalanceSettlement(from, to, amount, status, isDeleted);
        }

        public static GroupMoney Owing(IReadOnlyList<Guid> members, Guid creditorId, Guid debtorId)
        {
            BalanceExpense expense = EqualExpense(creditorId, Steps(1000, Currency.MKD), creditorId, debtorId);

            return new GroupMoney(members, [expense], []);
        }

        public static GroupMoney Settled(IReadOnlyList<Guid> members, Guid creditorId, Guid debtorId)
        {
            BalanceExpense expense = EqualExpense(creditorId, Steps(1000, Currency.MKD), creditorId, debtorId);
            BalanceSettlement payment = Settlement(debtorId, creditorId, Steps(500, Currency.MKD), SettlementStatus.Confirmed);

            return new GroupMoney(members, [expense], [payment]);
        }

        public static GroupMoney OneExpenseNobodyOwes(IReadOnlyList<Guid> members, Guid payerId)
        {
            BalanceExpense expense = EqualExpense(payerId, Steps(1000, Currency.MKD), payerId);

            return new GroupMoney(members, [expense], []);
        }

        public static GroupMoney OnlyADeletedExpense(IReadOnlyList<Guid> members, Guid payerId)
        {
            BalanceExpense deleted = EqualExpense(payerId, Steps(1000, Currency.MKD), payerId) with { IsDeleted = true };

            return new GroupMoney(members, [deleted], []);
        }

        public static GroupMoney OneDeletedAndOneCountedExpense(IReadOnlyList<Guid> members, Guid payerId, Guid debtorId)
        {
            BalanceExpense deletedDebt = EqualExpense(payerId, Steps(1000, Currency.MKD), payerId, debtorId) with { IsDeleted = true };
            BalanceExpense counted = EqualExpense(payerId, Steps(1000, Currency.MKD), payerId);

            return new GroupMoney(members, [deletedDebt, counted], []);
        }

        public static GroupMoney OnlyConfirmedPaymentsBothWays(IReadOnlyList<Guid> members, Guid firstId, Guid secondId)
        {
            BalanceSettlement there = Settlement(firstId, secondId, Steps(500, Currency.MKD), SettlementStatus.Confirmed);
            BalanceSettlement back = Settlement(secondId, firstId, Steps(500, Currency.MKD), SettlementStatus.Confirmed);

            return new GroupMoney(members, [], [there, back]);
        }

        public static GroupMoney PendingPaymentAfterOneExpenseNobodyOwes(IReadOnlyList<Guid> members, Guid payerId, Guid fromId, Guid toId)
        {
            BalanceExpense expense = EqualExpense(payerId, Steps(1000, Currency.MKD), payerId);
            BalanceSettlement payment = Settlement(fromId, toId, Steps(500, Currency.MKD), SettlementStatus.Pending);

            return new GroupMoney(members, [expense], [payment]);
        }

        public static GroupMoney PendingPayment(IReadOnlyList<Guid> members, Guid fromId, Guid toId)
        {
            BalanceSettlement payment = Settlement(fromId, toId, Steps(500, Currency.MKD), SettlementStatus.Pending);

            return new GroupMoney(members, [], [payment]);
        }
    }
}
