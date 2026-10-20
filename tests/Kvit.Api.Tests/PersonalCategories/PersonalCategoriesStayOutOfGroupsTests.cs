using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Categories;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalCategories
{
    public class PersonalCategoriesStayOutOfGroupsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task GroupCategoryList_CallerHasActiveAndArchivedOwnCategories_StillAnswersOnlyTheTenBuiltIns()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await PersonalCategorySteps.CreateAsync(group.Owner.Client, "Pets");
            await PersonalCategorySteps.CreateArchivedAsync(group.Owner.Client, "Car");

            HttpResponseMessage response = await CategoryRequests.ListAsync(group.Owner.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            List<JsonElement> rows = [.. body.GetProperty("categories").EnumerateArray()];
            Assert.Equal(BuiltInCategories.Keys, rows.Select(row => row.GetProperty("key").GetString()));
            Assert.All(rows, row =>
            {
                string[] propertyNames = [.. row.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
                Assert.Equal(["color", "emoji", "id", "key", "sortOrder"], propertyNames);
            });
        }

        [Fact]
        public async Task AddGroupExpense_WithTheCallersOwnCategory_Answers400CategoryInvalidAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(group.Owner.Client, "Pets");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { CategoryId = pets };
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CATEGORY_INVALID);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }
    }
}
