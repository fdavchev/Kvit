using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace Kvit.Api.Tests.BudgetCurrency
{
    public static class BudgetCurrencyRequests
    {
        public const string Path = "/api/me/budget-currency";
        public const string Mkd = "MKD";
        public const string Eur = "EUR";

        public static Task<HttpResponseMessage> SetAsync(HttpClient client, string? budgetTotalCurrency)
        {
            return client.PutAsJsonAsync(Path, new { budgetTotalCurrency }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> SetWithJsonTextAsync(HttpClient client, string json)
        {
            StringContent content = new(json, Encoding.UTF8, "application/json");

            return client.PutAsync(Path, content, TestContext.Current.CancellationToken);
        }
    }
}
