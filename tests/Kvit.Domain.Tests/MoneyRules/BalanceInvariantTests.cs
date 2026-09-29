using Kvit.Domain.MoneyRules;
using Xunit;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class BalanceInvariantTests
    {
        private const int Seed = 20260929;
        private const int GroupCount = 500;

        [Fact]
        public void RandomGroups_BalancesAlwaysAddUpToZeroInEachCurrency()
        {
            Random random = new(Seed);
            Currency[] currencies = [Currency.MKD, Currency.EUR];
            SplitType[] splitTypes = [SplitType.Equal, SplitType.Shares];
            SettlementStatus[] statuses = [SettlementStatus.Pending, SettlementStatus.Confirmed, SettlementStatus.Rejected, SettlementStatus.Cancelled];

            for (int groupNumber = 0; groupNumber < GroupCount; groupNumber++)
            {
                int memberCount = random.Next(2, 9);
                Guid[] members = [.. Enumerable.Range(0, memberCount).Select(_ => MoneyTestData.RandomMemberId(random))];
                List<BalanceExpense> expenses = [];
                List<BalanceSettlement> settlements = [];

                int expenseCount = random.Next(0, 30);
                for (int expenseNumber = 0; expenseNumber < expenseCount; expenseNumber++)
                {
                    Currency currency = currencies[random.Next(currencies.Length)];
                    Money amount = MoneyTestData.Steps(random.NextInt64(1, 100_000), currency);
                    Guid payer = members[random.Next(memberCount)];
                    Guid[] people = [.. members.Where(_ => random.Next(3) != 0)];
                    if (people.Length == 0)
                    {
                        people = [payer];
                    }

                    SplitType splitType = splitTypes[random.Next(splitTypes.Length)];
                    SplitInput[] inputs = [.. people.Select(person => new SplitInput(person, splitType == SplitType.Shares ? random.Next(1, 4) : 0))];
                    IReadOnlyList<SplitShare> shares = MoneyTestData.Split(splitType, amount, payer, inputs);
                    expenses.Add(new BalanceExpense(payer, amount, shares, IsDeleted: random.Next(10) == 0));
                }

                int settlementCount = random.Next(0, 10);
                for (int settlementNumber = 0; settlementNumber < settlementCount; settlementNumber++)
                {
                    int fromIndex = random.Next(memberCount);
                    int toIndex = (fromIndex + random.Next(1, memberCount)) % memberCount;
                    Currency currency = currencies[random.Next(currencies.Length)];
                    Money amount = MoneyTestData.Steps(random.NextInt64(1, 50_000), currency);
                    SettlementStatus status = statuses[random.Next(statuses.Length)];
                    settlements.Add(new BalanceSettlement(members[fromIndex], members[toIndex], amount, status, IsDeleted: random.Next(10) == 0));
                }

                BalanceSummary summary = Balances.Calculate(members, expenses, settlements);

                foreach (KeyValuePair<Currency, IReadOnlyList<MemberBalance>> currencyBalances in summary.ByCurrency)
                {
                    Assert.Equal(members, currencyBalances.Value.Select(balance => balance.MemberId));
                    Assert.Equal(0, currencyBalances.Value.Sum(balance => balance.Balance.MinorUnits));
                    Assert.All(currencyBalances.Value, balance => Assert.Equal(currencyBalances.Key, balance.Balance.Currency));
                }
            }
        }
    }
}
