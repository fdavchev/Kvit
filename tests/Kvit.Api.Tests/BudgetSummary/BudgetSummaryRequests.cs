using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.BudgetSummary
{
    public static class BudgetSummaryRequests
    {
        public const string Path = "/api/personal/summary";

        public static Task<HttpResponseMessage> GetAsync(HttpClient client, string month = PersonalEntryRequests.DefaultMonth)
        {
            return client.GetAsync($"{Path}?month={Uri.EscapeDataString(month)}", TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> GetWithoutMonthAsync(HttpClient client)
        {
            return client.GetAsync(Path, TestContext.Current.CancellationToken);
        }
    }
}
