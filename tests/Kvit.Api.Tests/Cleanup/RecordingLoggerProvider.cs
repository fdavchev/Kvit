using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Kvit.Api.Tests.Cleanup
{
    public sealed class RecordingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<RecordedLogEntry> _entries = new();

        public IReadOnlyList<RecordedLogEntry> AllEntries => [.. _entries];

        public IReadOnlyList<RecordedLogEntry> EntriesOf<T>()
        {
            string category = typeof(T).FullName ?? typeof(T).Name;

            return [.. _entries.Where(entry => entry.Category == category)];
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new RecordingLogger(categoryName, _entries);
        }

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(string _category, ConcurrentQueue<RecordedLogEntry> _entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                _entries.Enqueue(new RecordedLogEntry(_category, logLevel, formatter(state, exception), exception));
            }
        }
    }
}
