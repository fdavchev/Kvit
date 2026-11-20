using System.Diagnostics;
using System.Net;
using System.Text;

namespace Kvit.Api.Tests.ExchangeRates
{
    public sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<CancellationToken, Task<HttpResponseMessage>> _respond;

        private StubHttpMessageHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond)
        {
            _respond = respond;
        }

        public List<StubRequest> Requests { get; } = [];

        public static StubHttpMessageHandler Answering(HttpStatusCode statusCode, string body)
        {
            return new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            }));
        }

        public static StubHttpMessageHandler Throwing(Exception exception)
        {
            return new StubHttpMessageHandler(_ => throw exception);
        }

        public static StubHttpMessageHandler Hanging()
        {
            return new StubHttpMessageHandler(async cancellationToken =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                throw new UnreachableException("An endless delay ended without being cancelled.");
            });
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new StubRequest(request.Method, request.RequestUri));

            return _respond(cancellationToken);
        }
    }
}
