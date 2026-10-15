using Kvit.Domain.Entities;

namespace Kvit.Domain.Interfaces
{
    public interface IClosingResponseRepository
    {
        void Add(ClosingResponse response);

        Task<IReadOnlyList<ClosingResponse>> ResponsesOfAsync(Guid groupId, CancellationToken cancellationToken);

        Task RemoveAllOfAsync(Guid groupId, CancellationToken cancellationToken);
    }
}
