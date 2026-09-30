using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Kvit.Api.Tests.Hosting
{
    public sealed class UniqueClientAddressStartupFilter : IStartupFilter
    {
        private int _requestCount;

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (HttpContext context, RequestDelegate nextMiddleware) =>
                {
                    if (context.Connection.RemoteIpAddress is null)
                    {
                        context.Connection.RemoteIpAddress = NextAddress();
                    }

                    await nextMiddleware(context);
                });
                next(app);
            };
        }

        private IPAddress NextAddress()
        {
            int number = Interlocked.Increment(ref _requestCount);

            return new IPAddress([10, (byte)(number >> 16), (byte)(number >> 8), (byte)number]);
        }
    }
}
