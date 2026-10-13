using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Settlements;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Settlements
{
    public sealed class GetSettlementsQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetSettlementsQuery, SettlementListResponse>
    {
        public async Task<Result<SettlementListResponse>> Handle(GetSettlementsQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<SettlementListResponse>(userId.Error, userId.ErrorCode);
            }

            Guid? ownerUserId = await _context.OwnerOfGroupForMember(query.GroupId, userId.Value).SingleOrDefaultAsync(cancellationToken);
            if (ownerUserId is null)
            {
                return Group.NotFound<SettlementListResponse>(query.GroupId);
            }

            List<SettlementRow> settlements = await SettlementRowsOf(query.GroupId).ToListAsync(cancellationToken);

            return Result.Ok(new SettlementListResponse(settlements));
        }

        private IQueryable<SettlementRow> SettlementRowsOf(Guid groupId)
        {
            return _context.NamedSettlements(groupId)
                .AsNoTracking()
                .OrderByDescending(named => named.Settlement.CreatedAt)
                .ThenByDescending(named => named.Settlement.Id)
                .Select(named => new SettlementRow(
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
