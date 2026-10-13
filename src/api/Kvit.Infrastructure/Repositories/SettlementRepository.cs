using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class SettlementRepository(AppDbContext _context) : ISettlementRepository
    {
        public void Add(Settlement settlement)
        {
            _context.Settlements.Add(settlement);
        }

        public Task<Settlement?> FindByClientRequestIdAsync(Guid clientRequestId, CancellationToken cancellationToken)
        {
            return _context.Settlements
                .AsNoTracking()
                .SingleOrDefaultAsync(settlement => settlement.ClientRequestId == clientRequestId, cancellationToken);
        }

        public Task<Result<Settlement>> FindInGroupAsync(Guid groupId, Guid settlementId, CancellationToken cancellationToken)
        {
            return SingleOrNotFoundAsync(_context.Settlements.Where(settlement => settlement.DeletedAt == null), groupId, settlementId, cancellationToken);
        }

        public Task<Result<Settlement>> FindIncludingDeletedInGroupAsync(Guid groupId, Guid settlementId, CancellationToken cancellationToken)
        {
            return SingleOrNotFoundAsync(_context.Settlements, groupId, settlementId, cancellationToken);
        }

        private static async Task<Result<Settlement>> SingleOrNotFoundAsync(IQueryable<Settlement> settlements, Guid groupId, Guid settlementId, CancellationToken cancellationToken)
        {
            Settlement? settlement = await settlements.SingleOrDefaultAsync(candidate => candidate.Id == settlementId && candidate.GroupId == groupId, cancellationToken);
            if (settlement is null)
            {
                return Settlement.NotFound<Settlement>(settlementId);
            }

            return Result.Ok(settlement);
        }
    }
}
