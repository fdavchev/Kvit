using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.BudgetShares
{
    public static class BudgetShareSteps
    {
        public const string SharedTitle = "Dinner";
        public const long HalfOfOneThousandMkd = 50_000;

        public static readonly string[] GroupShareFieldNames = ["categoryId", "currency", "entryDate", "expenseId", "groupId", "groupName", "shareMinor", "title"];

        public static async Task SetAsync(SignedInUser user, Guid groupId, bool countsInBudget)
        {
            HttpResponseMessage response = await BudgetShareRequests.SetAsync(user.Client, groupId, countsInBudget);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public static async Task<ExpenseInput> SharedInputAsync(
            ExpensesApp app,
            ExpenseGroup group,
            string expenseDate = ExpenseInputs.DefaultDate,
            long amountMinor = ExpenseInputs.OneThousandMkd,
            string currency = ExpenseInputs.Mkd)
        {
            Guid food = await app.BuiltInCategoryIdAsync("food");

            return ExpenseInputs.Equal(amountMinor, currency, group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with
            {
                Title = SharedTitle,
                CategoryId = food,
                ExpenseDate = expenseDate,
            };
        }

        public static async Task<Guid> AddSharedExpenseAsync(
            ExpensesApp app,
            ExpenseGroup group,
            string expenseDate = ExpenseInputs.DefaultDate,
            long amountMinor = ExpenseInputs.OneThousandMkd,
            string currency = ExpenseInputs.Mkd)
        {
            ExpenseInput input = await SharedInputAsync(app, group, expenseDate, amountMinor, currency);

            return await app.AddExpenseAsync(group.Owner, group.GroupId, input);
        }

        public static async Task<JsonElement> GroupSharesAsync(HttpClient client, string month = PersonalEntryRequests.DefaultMonth)
        {
            JsonElement body = await PersonalEntrySteps.MonthAsync(client, month);

            return body.GetProperty("groupShares");
        }

        public static async Task<List<Guid>> ListedExpenseIdsAsync(HttpClient client, string month = PersonalEntryRequests.DefaultMonth)
        {
            JsonElement shares = await GroupSharesAsync(client, month);

            return [.. shares.EnumerateArray().Select(row => row.GetProperty("expenseId").GetGuid()).Order()];
        }

        public static List<Guid> Sorted(params Guid[] expenseIds)
        {
            return [.. expenseIds.Order()];
        }

        public static JsonElement RowOfExpense(JsonElement shares, Guid expenseId)
        {
            return shares.EnumerateArray().Single(row => row.GetProperty("expenseId").GetGuid() == expenseId);
        }

        public static async Task<bool> CountsInBudgetOfAsync(HttpClient client, Guid groupId)
        {
            HttpResponseMessage response = await GroupRequests.GetAsync(client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("countsInBudget").GetBoolean();
        }

        public static async Task<bool> MemberCountsAsync(MigratedDatabase database, Guid memberRowId)
        {
            string? value = await GroupRows.MemberRowValueAsync(database, memberRowId, "counts_in_budget");

            return bool.Parse(value ?? throw new InvalidOperationException($"Member row {memberRowId} has no counts_in_budget value."));
        }

        public static async Task<bool> ShareCountsAsync(MigratedDatabase database, Guid expenseId, Guid memberRowId)
        {
            List<string?> values = await database.QueryAsync(
                "SELECT counts_in_budget::text FROM expense_shares WHERE expense_id = @expense_id AND member_id = @member_id",
                new NpgsqlParameter("expense_id", expenseId),
                new NpgsqlParameter("member_id", memberRowId));

            return bool.Parse(Assert.Single(values)!);
        }
    }
}
