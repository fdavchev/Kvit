using Kvit.Application.Dispatching;
using Kvit.Domain.Results;

namespace Kvit.Api.Tests.Dispatching
{
    public sealed class RecordCommandHandler : ICommandHandler<RecordCommand>
    {
        public Task<Result> Handle(RecordCommand command, CancellationToken cancellationToken)
        {
            command.HandledBy.Add(nameof(RecordCommandHandler));
            return Task.FromResult(Result.Ok());
        }
    }
}
