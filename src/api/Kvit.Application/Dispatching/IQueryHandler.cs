using Kvit.Domain.Results;

namespace Kvit.Application.Dispatching
{
    public interface IQueryHandler<in TQuery, TResult>
    {
        Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken);
    }
}
