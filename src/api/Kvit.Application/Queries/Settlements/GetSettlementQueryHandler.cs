using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Settlements;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Settlements
{
    public sealed class GetSettlementQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetSettlementQuery, SettlementResponse>
    {
        public async Task<Result<SettlementResponse>> Handle(GetSettlementQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<SettlementResponse>(userId.Error, userId.ErrorCode);
            }

            Guid? ownerUserId = await _context.OwnerOfGroupForMember(query.GroupId, userId.Value).SingleOrDefaultAsync(cancellationToken);
            if (ownerUserId is null)
            {
                return Group.NotFound<SettlementResponse>(query.GroupId);
            }

            SettlementResponse? settlement = await SettlementResponseOf(query.GroupId, query.SettlementId).SingleOrDefaultAsync(cancellationToken);
            if (settlement is null)
            {
                return Settlement.NotFound<SettlementResponse>(query.SettlementId);
            }

            return Result.Ok(settlement);
        }

        private IQueryable<SettlementResponse> SettlementResponseOf(Guid groupId, Guid settlementId)
        {
            return _context.NamedSettlements(groupId)
                .AsNoTracking()
                .Where(named => named.Settlement.Id == settlementId)
                .Select(named => new SettlementResponse(
                    named.Settlement.Id,
                    named.Settlement.FromMemberId,
                    named.FromName,
                    named.Settlement.ToMemberId,
                    named.ToName,
                    named.Settlement.AmountMinor,
                    named.Settlement.Currency,
                    named.Settlement.Status,
                    named.Settlement.RecordedByUserId,
                    named.Settlement.CreatedAt,
                    named.Settlement.ResolvedAt));
        }
    }
}
