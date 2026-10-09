using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public static class ExpenseEndpoints
    {
        public const string ExpensePlaceholder = "{expense}";

        public static TheoryData<string, string> All => new()
        {
            { "GET", "/expenses" },
            { "POST", "/expenses" },
            { "GET", "/expenses/deleted" },
            { "GET", "/expenses/{expense}" },
            { "PUT", "/expenses/{expense}" },
            { "DELETE", "/expenses/{expense}" },
            { "POST", "/expenses/{expense}/restore" },
            { "GET", "/activity" },
        };

        public static TheoryData<string, string> WithExpenseId => new()
        {
            { "GET", "/expenses/{expense}" },
            { "PUT", "/expenses/{expense}" },
            { "DELETE", "/expenses/{expense}" },
            { "POST", "/expenses/{expense}/restore" },
        };

        public static string PathFor(string groupSegment, string suffix, string expenseSegment)
        {
            return $"/api/groups/{groupSegment}{suffix.Replace(ExpensePlaceholder, expenseSegment)}";
        }

        public static async Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string suffix, Guid groupId, Guid expenseId)
        {
            return await SendToSegmentsAsync(client, method, suffix, groupId.ToString(), expenseId.ToString());
        }

        public static async Task<HttpResponseMessage> SendToSegmentsAsync(HttpClient client, string method, string suffix, string groupSegment, string expenseSegment)
        {
            string path = PathFor(groupSegment, suffix, expenseSegment);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, Guid.CreateVersion7(), Guid.CreateVersion7());

            return await GroupRequests.SendAsync(client, method, path, ExpenseRequests.AddBody(input, Guid.CreateVersion7()));
        }
    }
}
