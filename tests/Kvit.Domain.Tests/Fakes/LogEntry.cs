using Microsoft.Extensions.Logging;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
