using Kvit.Api.Tests.Cleanup;
using Kvit.Api.Tests.Hosting;
using Kvit.Contracts.Persistence;
using Kvit.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kvit.Api.Tests.Application
{
    public class UnitOfWorkClashLogTests : IClassFixture<KvitApiFactory>
    {
        private readonly KvitApiFactory _factory;
        private readonly RecordingLoggerProvider _logs = new();

        public UnitOfWorkClashLogTests(KvitApiFactory factory)
        {
            _factory = factory;
            _factory.AdditionalTestServices = services => services.AddSingleton<ILoggerProvider>(_logs);
        }

        [Fact]
        public void LogCaughtClash_WritesAWarningWithTheClashAndWhatHappensNext()
        {
            DeadlockDetectedException deadlock = new(new InvalidOperationException("Deadlock."));
            using IServiceScope scope = _factory.Services.CreateScope();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            unitOfWork.LogCaughtClash(deadlock, "the transaction runs once more");

            RecordedLogEntry entry = Assert.Single(_logs.EntriesOf<UnitOfWork>());
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.Same(deadlock, entry.Exception);
            Assert.Contains(nameof(DeadlockDetectedException), entry.Message, StringComparison.Ordinal);
            Assert.Contains("the transaction runs once more", entry.Message, StringComparison.Ordinal);
        }
    }
}
