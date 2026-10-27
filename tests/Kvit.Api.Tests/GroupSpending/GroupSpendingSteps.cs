using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.PersonalEntries;

namespace Kvit.Api.Tests.GroupSpending
{
    public static class GroupSpendingSteps
    {
        public const long MkdBound = 999_999_999_900;
        public const long EurBound = 999_999_999_999;

        public static readonly Guid Food = PersonalEntryRequests.BuiltInFoodId;
        public static readonly Guid Transport = PersonalEntryRequests.BuiltInTransportId;

        public static Task<Guid> AddAsync(
            ExpensesApp app,
            ExpenseGroup group,
            long amountMinor,
            string currency = ExpenseInputs.Mkd,
            Guid? categoryId = null)
        {
            ExpenseInput input = ExpenseInputs.Equal(amountMinor, currency, group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { CategoryId = categoryId };

            return app.AddExpenseAsync(group.Owner, group.GroupId, input);
        }

        public static Task<Guid> AddPaidAloneAsync(
            ExpensesApp app,
            ExpenseGroup group,
            long amountMinor,
            string currency = ExpenseInputs.Mkd,
            Guid? categoryId = null)
        {
            ExpenseInput input = ExpenseInputs.Equal(amountMinor, currency, group.OwnerRowId, group.OwnerRowId) with { CategoryId = categoryId };

            return app.AddExpenseAsync(group.Owner, group.GroupId, input);
        }
    }
}
