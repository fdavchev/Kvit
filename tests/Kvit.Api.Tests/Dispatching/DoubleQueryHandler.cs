using Kvit.Application.Dispatching;
using Kvit.Domain.Results;

namespace Kvit.Api.Tests.Dispatching
{
    public sealed class DoubleQueryHandler : IQueryHandler<DoubleQuery, int>
    {
        public Task<Result<int>> Handle(DoubleQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Ok(query.Number * 2));
        }
    }
}
