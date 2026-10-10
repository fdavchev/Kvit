using Kvit.Api.Tests.Expenses;

namespace Kvit.Api.Tests.Balances
{
    public static class BalanceScenario
    {
        public static readonly string[] PlainNames = ["Marko", "Cveta"];

        public static readonly long[] MkdBalancesInJoiningOrder = [-23_300, 66_600, -53_300, 10_000];

        public static readonly long[] EurBalancesInJoiningOrder = [1_667, -333, -2_000, 666];

        public static Task<ExpenseGroup> CreateGroupAsync(ExpensesApp app)
        {
            return app.CreateExpenseGroupAsync(PlainNames);
        }

        public static async Task AddMixedExpensesAsync(ExpensesApp app, ExpenseGroup group)
        {
            Guid ana = group.OwnerRowId;
            Guid bojan = group.MemberRowId;
            Guid marko = group.PlainRowIds[0];
            Guid cveta = group.PlainRowIds[1];

            await app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, ana, ana, bojan, marko, cveta));
            await app.AddExpenseAsync(
                group.Member,
                group.GroupId,
                ExpenseInputs.Of("Exact", 90_000, ExpenseInputs.Mkd, bojan, new ShareInput(ana, 20_000), new ShareInput(bojan, 10_000), new ShareInput(marko, 60_000)));
            await app.AddExpenseAsync(
                group.Owner,
                group.GroupId,
                ExpenseInputs.Of("Percentage", 100_000, ExpenseInputs.Mkd, marko, new ShareInput(ana, 5_000), new ShareInput(bojan, 3_000), new ShareInput(cveta, 2_000)));
            await app.AddExpenseAsync(
                group.Owner,
                group.GroupId,
                ExpenseInputs.Of("Shares", 60_000, ExpenseInputs.Mkd, cveta, new ShareInput(ana, 1), new ShareInput(bojan, 2), new ShareInput(marko, 3)));
            await app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(100_000, ExpenseInputs.Mkd, bojan, ana, bojan, marko));
            await app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_000, ExpenseInputs.Eur, cveta, ana, bojan, cveta));
            await app.AddExpenseAsync(
                group.Owner,
                group.GroupId,
                ExpenseInputs.Of("Exact", 2_500, ExpenseInputs.Eur, ana, new ShareInput(ana, 500), new ShareInput(marko, 2_000)));
        }

        public static List<ExpectedPayment> ExpectedMkdPayments(ExpenseGroup group)
        {
            Guid ana = group.OwnerRowId;
            Guid bojan = group.MemberRowId;
            Guid marko = group.PlainRowIds[0];
            Guid cveta = group.PlainRowIds[1];

            return
            [
                new ExpectedPayment(marko, bojan, 53_300),
                new ExpectedPayment(ana, bojan, 13_300),
                new ExpectedPayment(ana, cveta, 10_000),
            ];
        }

        public static List<ExpectedPayment> ExpectedEurPayments(ExpenseGroup group)
        {
            Guid ana = group.OwnerRowId;
            Guid bojan = group.MemberRowId;
            Guid marko = group.PlainRowIds[0];
            Guid cveta = group.PlainRowIds[1];

            return
            [
                new ExpectedPayment(marko, ana, 1_667),
                new ExpectedPayment(bojan, cveta, 333),
                new ExpectedPayment(marko, cveta, 333),
            ];
        }
    }
}
