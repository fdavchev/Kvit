using Kvit.Domain.Results;

namespace Kvit.Application.Dispatching
{
    public interface ICommandHandler<in TCommand>
    {
        Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
    }
}
