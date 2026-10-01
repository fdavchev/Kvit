using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Kvit.Api.Tests.Scripts
{
    public sealed class FakeRenderServer : IAsyncDisposable
    {
        public const string ServiceId = "srv-test";
        public const string DeployId = "dep-test";
        public const string RejectionMessage = "fake-rejection-message";
        public const string DeploysPath = $"/v1/services/{ServiceId}/deploys";

        private const string TriggerResponseStatus = "created";

        private readonly WebApplication _app;
        private readonly HttpStatusCode _triggerStatusCode;
        private readonly IReadOnlyList<string> _pollStatuses;
        private readonly ConcurrentQueue<RecordedRequest> _requests = new();
        private int _pollCount = -1;

        private FakeRenderServer(HttpStatusCode triggerStatusCode, IReadOnlyList<string> pollStatuses)
        {
            _triggerStatusCode = triggerStatusCode;
            _pollStatuses = pollStatuses;

            WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            _app = builder.Build();
            _app.Run(HandleAsync);
        }

        public string BaseUrl => _app.Urls.Single();

        public IReadOnlyList<RecordedRequest> Requests => [.. _requests];

        public static async Task<FakeRenderServer> StartAsync(HttpStatusCode triggerStatusCode, params string[] pollStatuses)
        {
            FakeRenderServer server = new(triggerStatusCode, pollStatuses);
            await server._app.StartAsync();
            return server;
        }

        public async ValueTask DisposeAsync()
        {
            await _app.DisposeAsync();
        }

        private async Task HandleAsync(HttpContext context)
        {
            string body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            string path = context.Request.Path.Value ?? string.Empty;
            _requests.Enqueue(new RecordedRequest(context.Request.Method, path + context.Request.QueryString, context.Request.Headers.Authorization.ToString(), body));

            if (HttpMethods.IsPost(context.Request.Method) && path == DeploysPath)
            {
                await RespondAsync(context, _triggerStatusCode, TriggerResponse());
            }
            else if (HttpMethods.IsGet(context.Request.Method) && path == $"{DeploysPath}/{DeployId}")
            {
                await RespondAsync(context, HttpStatusCode.OK, JsonSerializer.Serialize(new { id = DeployId, status = NextPollStatus() }));
            }
            else if (HttpMethods.IsGet(context.Request.Method) && path == DeploysPath)
            {
                await RespondAsync(context, HttpStatusCode.OK, JsonSerializer.Serialize(new[] { new { deploy = new { id = DeployId, status = _pollStatuses[0] }, cursor = "cursor-1" } }));
            }
            else
            {
                await RespondAsync(context, HttpStatusCode.NotFound, JsonSerializer.Serialize(new { message = $"The fake server has no route for {context.Request.Method} {path}." }));
            }
        }

        private string TriggerResponse()
        {
            if (_triggerStatusCode == HttpStatusCode.Accepted)
            {
                return string.Empty;
            }

            if ((int)_triggerStatusCode is >= 200 and < 300)
            {
                return JsonSerializer.Serialize(new { id = DeployId, status = TriggerResponseStatus });
            }

            return JsonSerializer.Serialize(new { message = RejectionMessage });
        }

        private string NextPollStatus()
        {
            int index = Interlocked.Increment(ref _pollCount);
            return _pollStatuses[Math.Min(index, _pollStatuses.Count - 1)];
        }

        private static async Task RespondAsync(HttpContext context, HttpStatusCode statusCode, string json)
        {
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }
}
