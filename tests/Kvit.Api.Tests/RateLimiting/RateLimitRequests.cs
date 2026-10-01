using System.Text;
using Kvit.Api.Tests.Auth;
using Xunit;

namespace Kvit.Api.Tests.RateLimiting
{
    public static class RateLimitRequests
    {
        private const string ForwardedForHeader = "X-Forwarded-For";

        public static Task<HttpResponseMessage> PostCountedAttemptAsync(HttpClient client, string path, string? forwardedFor)
        {
            string json = path == AuthRequests.RegisterPath ? AuthRequests.RegistrationJson(RegistrationForm.Valid()) : "{}";

            return PostJsonAsync(client, path, json, forwardedFor);
        }

        public static Task<HttpResponseMessage> PostRegistrationAsync(HttpClient client, RegistrationForm form, string? forwardedFor)
        {
            return PostJsonAsync(client, AuthRequests.RegisterPath, AuthRequests.RegistrationJson(form), forwardedFor);
        }

        public static void AssertRetryAfterInWholeSeconds(HttpResponseMessage refused)
        {
            Assert.True(refused.Headers.TryGetValues("Retry-After", out IEnumerable<string>? values));
            Assert.True(int.TryParse(values.Single(), out int seconds), $"Retry-After is not whole seconds: {values.Single()}");
            Assert.True(seconds > 0, $"Retry-After must be greater than zero, got {seconds}");
        }

        private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, string json, string? forwardedFor)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, path)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            if (forwardedFor is not null)
            {
                request.Headers.Add(ForwardedForHeader, forwardedFor);
            }

            return await client.SendAsync(request, TestContext.Current.CancellationToken);
        }
    }
}
