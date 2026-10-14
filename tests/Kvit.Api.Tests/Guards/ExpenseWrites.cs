using Kvit.Api.Tests.Expenses;
using Kvit.Domain.Results;

namespace Kvit.Api.Tests.Guards
{
    public static class ExpenseWrites
    {
        public static IReadOnlyList<GuardedWrite> All { get; } =
        [
            new GuardedWrite
            {
                Name = "AddExpense",
                Who = WhoMayCall.OwnerAndMembers,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                PrepareAsync = (_, group) => Task.FromResult<SendWrite>(
                    caller => ExpenseRequests.AddAsync(caller, group.GroupId, GuardSteps.MemberExpenseInput(group))),
            },
            new GuardedWrite
            {
                Name = "EditExpense",
                Who = WhoMayCall.OwnerAndMembers,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                CodeWhenAnotherMemberTries = ResultCodes.EXPENSE_NOT_ALLOWED,
                PrepareAsync = async (app, group) =>
                {
                    Guid expenseId = await GuardSteps.AddBalancedPairOfExpensesAsync(app, group);

                    return caller => ExpenseRequests.EditAsync(caller, group.GroupId, expenseId, GuardSteps.MemberExpenseInput(group, GuardSteps.BiggerExpenseMinor));
                },
            },
            new GuardedWrite
            {
                Name = "DeleteExpense",
                Who = WhoMayCall.OwnerAndMembers,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                CodeWhenAnotherMemberTries = ResultCodes.EXPENSE_NOT_ALLOWED,
                PrepareAsync = async (app, group) =>
                {
                    Guid expenseId = await GuardSteps.AddBalancedPairOfExpensesAsync(app, group);

                    return caller => ExpenseRequests.DeleteAsync(caller, group.GroupId, expenseId);
                },
            },
            new GuardedWrite
            {
                Name = "RestoreExpense",
                Who = WhoMayCall.OwnerAndMembers,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                CodeWhenAnotherMemberTries = ResultCodes.EXPENSE_NOT_ALLOWED,
                PrepareAsync = async (app, group) =>
                {
                    Guid expenseId = await GuardSteps.AddAndDeleteMemberExpenseAsync(app, group);

                    return caller => ExpenseRequests.RestoreAsync(caller, group.GroupId, expenseId);
                },
            },
        ];
    }
}
