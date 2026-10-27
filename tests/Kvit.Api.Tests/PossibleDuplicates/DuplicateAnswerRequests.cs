using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public static class DuplicateAnswerRequests
    {
        public const string Path = "/api/personal/duplicate-answers";
        public const string SameAnswer = "Same";
        public const string NewAnswer = "New";
        public const string InvalidAnswerCode = "DUPLICATE_ANSWER_INVALID";

        public static TheoryData<string> AllEndpoints => ["put", "delete"];

        public static Task<HttpResponseMessage> PutAsync(HttpClient client, Guid entryId, Guid expenseId, string answer = SameAnswer)
        {
            return client.PutAsJsonAsync(Path, new { entryId, expenseId, answer }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> PutWithJsonTextAsync(HttpClient client, string json)
        {
            StringContent content = new(json, Encoding.UTF8, "application/json");

            return client.PutAsync(Path, content, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, Guid entryId, Guid expenseId)
        {
            HttpRequestMessage request = new(HttpMethod.Delete, Path)
            {
                Content = JsonContent.Create(new { entryId, expenseId }),
            };

            return client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> SendAsync(HttpClient client, string endpoint, Guid entryId, Guid expenseId)
        {
            return endpoint switch
            {
                "put" => PutAsync(client, entryId, expenseId),
                "delete" => DeleteAsync(client, entryId, expenseId),
                _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Not a duplicate answer endpoint."),
            };
        }
    }
}
