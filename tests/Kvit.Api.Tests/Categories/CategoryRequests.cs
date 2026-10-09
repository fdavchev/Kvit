using Xunit;

namespace Kvit.Api.Tests.Categories
{
    public static class CategoryRequests
    {
        public const string CategoriesPath = "/api/categories";

        public static Task<HttpResponseMessage> ListAsync(HttpClient client)
        {
            return client.GetAsync(CategoriesPath, TestContext.Current.CancellationToken);
        }
    }
}
