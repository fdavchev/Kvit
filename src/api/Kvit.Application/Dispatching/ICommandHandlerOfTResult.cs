using Kvit.Domain.Results;

namespace Kvit.Application.Dispatching
{
    public interface ICommandHandler<in TCommand, TResult>
    {
        Task<Result<TResult>> Handle(TCommand command, CancellationToken cancellationToken);
    }
}
