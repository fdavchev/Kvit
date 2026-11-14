using Microsoft.Extensions.Logging;

namespace Kvit.Api.Tests.Cleanup
{
    public sealed record RecordedLogEntry(string Category, LogLevel Level, string Message, Exception? Exception);
}
