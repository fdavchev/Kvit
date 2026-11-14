using Kvit.Application.Dispatching;
using Kvit.Contracts.OwnerTools;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.OwnerTools
{
    public sealed class GetOwnerToolsLogQueryHandler(AppDbContext _context) : IQueryHandler<GetOwnerToolsLogQuery, IReadOnlyList<OwnerToolsLogRow>>
    {
        public async Task<Result<IReadOnlyList<OwnerToolsLogRow>>> Handle(GetOwnerToolsLogQuery query, CancellationToken cancellationToken)
        {
            List<OwnerToolsLogRow> rows = await OwnerToolsLogRowsOf(AdminAction.LogLimitOf(query.Limit)).ToListAsync(cancellationToken);

            return Result.Ok<IReadOnlyList<OwnerToolsLogRow>>(rows);
        }

        private IQueryable<OwnerToolsLogRow> OwnerToolsLogRowsOf(int limit)
        {
            return _context.AdminActions
                .AsNoTracking()
                .OrderByDescending(adminAction => adminAction.CreatedAt)
                .ThenByDescending(adminAction => adminAction.Id)
                .Take(limit)
                .Select(adminAction => new OwnerToolsLogRow(
                    adminAction.Id,
                    adminAction.CreatedAt,
                    _context.Users.Where(user => user.Id == adminAction.AdminUserId).Select(user => user.DisplayName).First(),
                    adminAction.Action,
                    adminAction.TargetUserId != null ? "person" : "group",
                    adminAction.TargetLabel,
                    adminAction.Reason));
        }
    }
}
