using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class EditExpenseArchivedCategoryTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Edit_AnExpenseThatKeepsItsCategoryAfterTheCategoryWasArchived_Answers204AndSavesTheEdit()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner", CategoryId = food };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ArchiveAsync(food);
            try
            {
                HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                Assert.Equal("Lunch", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
                Assert.Equal(food.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "category_id"));
            }
            finally
            {
                await UnarchiveAsync(food);
            }
        }

        [Fact]
        public async Task Edit_MovingAnExpenseIntoAnArchivedCategory_Answers400CategoryInvalid()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ArchiveAsync(food);
            try
            {
                HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { CategoryId = food });

                await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CATEGORY_INVALID);
            }
            finally
            {
                await UnarchiveAsync(food);
            }
        }

        private Task ArchiveAsync(Guid categoryId)
        {
            return _app.ExecuteAsync("UPDATE categories SET archived_at = now() WHERE id = @id", new NpgsqlParameter("id", categoryId));
        }

        private Task UnarchiveAsync(Guid categoryId)
        {
            return _app.ExecuteAsync("UPDATE categories SET archived_at = NULL WHERE id = @id", new NpgsqlParameter("id", categoryId));
        }
    }
}
