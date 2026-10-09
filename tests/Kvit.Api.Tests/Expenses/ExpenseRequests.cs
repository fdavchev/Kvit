using System.Net.Http.Json;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public static class ExpenseRequests
    {
        public const string OneBillPath = "/api/groups/one-bill";

        public static string ExpensesPathOf(Guid groupId)
        {
            return $"/api/groups/{groupId}/expenses";
        }

        public static string ExpensePathOf(Guid groupId, Guid expenseId)
        {
            return $"{ExpensesPathOf(groupId)}/{expenseId}";
        }

        public static string DeletedPathOf(Guid groupId)
        {
            return $"{ExpensesPathOf(groupId)}/deleted";
        }

        public static string ActivityPathOf(Guid groupId)
        {
            return $"/api/groups/{groupId}/activity";
        }

        public static Task<HttpResponseMessage> AddAsync(HttpClient client, Guid groupId, ExpenseInput input, Guid? clientRequestId = null)
        {
            return client.PostAsJsonAsync(ExpensesPathOf(groupId), AddBody(input, clientRequestId ?? Guid.CreateVersion7()), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ListAsync(HttpClient client, Guid groupId)
        {
            return client.GetAsync(ExpensesPathOf(groupId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> GetAsync(HttpClient client, Guid groupId, Guid expenseId)
        {
            return client.GetAsync(ExpensePathOf(groupId, expenseId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> EditAsync(HttpClient client, Guid groupId, Guid expenseId, ExpenseInput input)
        {
            return client.PutAsJsonAsync(ExpensePathOf(groupId, expenseId), input, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, Guid groupId, Guid expenseId)
        {
            return client.DeleteAsync(ExpensePathOf(groupId, expenseId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> RestoreAsync(HttpClient client, Guid groupId, Guid expenseId)
        {
            return client.PostAsync($"{ExpensePathOf(groupId, expenseId)}/restore", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ListDeletedAsync(HttpClient client, Guid groupId)
        {
            return client.GetAsync(DeletedPathOf(groupId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ActivityAsync(HttpClient client, Guid groupId)
        {
            return client.GetAsync(ActivityPathOf(groupId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> OneBillAsync(HttpClient client, OneBillInput input, Guid? clientRequestId = null)
        {
            return client.PostAsJsonAsync(OneBillPath, OneBillBody(input, clientRequestId ?? Guid.CreateVersion7()), TestContext.Current.CancellationToken);
        }

        public static object AddBody(ExpenseInput input, Guid clientRequestId)
        {
            return new
            {
                clientRequestId,
                input.Title,
                input.Note,
                input.AmountMinor,
                input.Currency,
                input.ExpenseDate,
                input.CategoryId,
                input.PaidByMemberId,
                input.SplitType,
                input.Shares,
            };
        }

        public static object OneBillBody(OneBillInput input, Guid clientRequestId)
        {
            return new
            {
                clientRequestId,
                input.Title,
                input.GroupName,
                input.Emoji,
                input.Names,
                input.Note,
                input.AmountMinor,
                input.Currency,
                input.ExpenseDate,
                input.CategoryId,
                input.PaidByPersonIndex,
                input.SplitType,
                input.Shares,
            };
        }
    }
}
