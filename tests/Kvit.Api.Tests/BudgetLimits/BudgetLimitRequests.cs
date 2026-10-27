using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace Kvit.Api.Tests.BudgetLimits
{
    public static class BudgetLimitRequests
    {
        public const string MonthPath = "/api/personal/limits/month";
        public const string CategoriesPath = "/api/personal/limits/categories";
        public const long OneThousandMkd = 100_000;
        public const long LargestWholeDenarLimit = 999_999_999_900;
        public const long SmallestLimit = 100;

        public static TheoryData<string> AllEndpoints => ["set-month", "set-category", "delete-month", "delete-category"];

        public static string CategoryPathOf(Guid categoryId)
        {
            return $"{CategoriesPath}/{categoryId}";
        }

        public static Task<HttpResponseMessage> SetMonthAsync(HttpClient client, long limitMinor = OneThousandMkd)
        {
            return client.PutAsJsonAsync(MonthPath, new { limitMinor }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> SetCategoryAsync(HttpClient client, Guid categoryId, long limitMinor = OneThousandMkd)
        {
            return client.PutAsJsonAsync(CategoryPathOf(categoryId), new { limitMinor }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> SetMonthWithJsonTextAsync(HttpClient client, string json)
        {
            StringContent content = new(json, Encoding.UTF8, "application/json");

            return client.PutAsync(MonthPath, content, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> SetCategoryWithJsonTextAsync(HttpClient client, Guid categoryId, string json)
        {
            StringContent content = new(json, Encoding.UTF8, "application/json");

            return client.PutAsync(CategoryPathOf(categoryId), content, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> DeleteMonthAsync(HttpClient client)
        {
            return client.DeleteAsync(MonthPath, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> DeleteCategoryAsync(HttpClient client, Guid categoryId)
        {
            return client.DeleteAsync(CategoryPathOf(categoryId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> SendAsync(HttpClient client, string endpoint, Guid categoryId)
        {
            return endpoint switch
            {
                "set-month" => SetMonthAsync(client),
                "set-category" => SetCategoryAsync(client, categoryId),
                "delete-month" => DeleteMonthAsync(client),
                "delete-category" => DeleteCategoryAsync(client, categoryId),
                _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Not a budget limit endpoint."),
            };
        }
    }
}
