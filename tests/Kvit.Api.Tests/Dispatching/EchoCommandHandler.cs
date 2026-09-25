using Kvit.Application.Dispatching;
using Kvit.Domain.Results;

namespace Kvit.Api.Tests.Dispatching
{
    public sealed class EchoCommandHandler : ICommandHandler<EchoCommand, string>
    {
        public Task<Result<string>> Handle(EchoCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Ok($"{nameof(EchoCommandHandler)}: {command.Text}"));
        }
    }
}
