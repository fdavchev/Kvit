using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalCategories;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Api.Tests.RecurringIncomes;

namespace Kvit.Api.Tests.AccountDeletion
{
    public static class PersonalDataSteps
    {
        public const decimal MkdPerEur = 61.5610m;

        private static readonly DateOnly GeneratedIncomeDate = new(2026, 10, 1);
        private static readonly DateOnly LimitMonth = new(2026, 10, 1);

        public static async Task CreateFullSetAsync(ExpensesApp app, SignedInUser user, Guid expenseForTheAnswer)
        {
            await BudgetCurrencySteps.UseRateAsync(app, MkdPerEur);
            Guid coffeeCategoryId = await PersonalCategorySteps.CreateAsync(user.Client, "Coffee beans");
            await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Old hobby");
            Guid coffeeEntryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(coffeeCategoryId, note: "Coffee for the office"));
            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(note: "Bonus"));
            await PersonalEntrySteps.AddAndDeleteAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, note: "Deleted entry"));
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());
            await RecurringIncomeRows.InsertEntryAsync(app, user.UserId, recurringIncomeId, GeneratedIncomeDate);
            await BudgetLimitRows.InsertAsync(app, user.UserId, null, LimitMonth, 800_000);
            await BudgetLimitRows.InsertAsync(app, user.UserId, coffeeCategoryId, LimitMonth, 300_000);
            await DuplicateAnswerRows.InsertAsync(app, user.UserId, coffeeEntryId, expenseForTheAnswer);
        }
    }
}
