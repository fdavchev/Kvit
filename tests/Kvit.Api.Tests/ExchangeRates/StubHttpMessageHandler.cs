using System.Net;
using System.Text;

namespace Kvit.Api.Tests.ExchangeRates
{
    public sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _respond;

        private StubHttpMessageHandler(Func<HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        public List<StubRequest> Requests { get; } = [];

        public static StubHttpMessageHandler Answering(HttpStatusCode statusCode, string body)
        {
            return new StubHttpMessageHandler(() => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }

        public static StubHttpMessageHandler Throwing(Exception exception)
        {
            return new StubHttpMessageHandler(() => throw exception);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new StubRequest(request.Method, request.RequestUri));

            return Task.FromResult(_respond());
        }
    }
}
