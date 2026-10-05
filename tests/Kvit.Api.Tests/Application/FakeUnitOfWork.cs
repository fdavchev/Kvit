using Kvit.Contracts.Persistence;

namespace Kvit.Api.Tests.Application
{
    public sealed class FakeUnitOfWork : IUnitOfWork
    {
        public const string OpenedCall = "opened";
        public const string CommittedCall = "committed";
        public const string RolledBackCall = "rolled back";

        public List<string> Calls { get; } = [];

        public List<CancellationToken> TokensReceived { get; } = [];

        public Task OpenTransactionAsync(CancellationToken cancellationToken)
        {
            return Record(OpenedCall, cancellationToken);
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            return Record(CommittedCall, cancellationToken);
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            return Record(RolledBackCall, cancellationToken);
        }

        private Task Record(string call, CancellationToken cancellationToken)
        {
            Calls.Add(call);
            TokensReceived.Add(cancellationToken);

            return Task.CompletedTask;
        }
    }
}
