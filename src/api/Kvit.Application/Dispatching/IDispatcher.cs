using Kvit.Domain.Results;

namespace Kvit.Application.Dispatching
{
    public interface IDispatcher
    {
        Task<Result> Send<TCommand>(TCommand command, CancellationToken cancellationToken);

        Task<Result<TResult>> Send<TCommand, TResult>(TCommand command, CancellationToken cancellationToken);

        Task<Result<TResult>> Query<TQuery, TResult>(TQuery query, CancellationToken cancellationToken);
    }
}
