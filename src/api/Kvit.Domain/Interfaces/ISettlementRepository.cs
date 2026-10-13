using Kvit.Domain.Entities;
using Kvit.Domain.Results;

namespace Kvit.Domain.Interfaces
{
    public interface ISettlementRepository
    {
        void Add(Settlement settlement);

        Task<Settlement?> FindByClientRequestIdAsync(Guid clientRequestId, CancellationToken cancellationToken);

        Task<Result<Settlement>> FindInGroupAsync(Guid groupId, Guid settlementId, CancellationToken cancellationToken);

        Task<Result<Settlement>> FindIncludingDeletedInGroupAsync(Guid groupId, Guid settlementId, CancellationToken cancellationToken);
    }
}
