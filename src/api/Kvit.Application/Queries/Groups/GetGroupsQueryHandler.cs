using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Groups;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Groups
{
    public sealed class GetGroupsQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context,
        TimeProvider _timeProvider) : IQueryHandler<GetGroupsQuery, GroupListResponse>
    {
        public async Task<Result<GroupListResponse>> Handle(GetGroupsQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<GroupListResponse>(userId.Error, userId.ErrorCode);
            }

            List<GroupListDto> memberGroups = await GroupListDtosOf(userId.Value).ToListAsync(cancellationToken);
            DateTimeOffset now = _timeProvider.GetUtcNow();
            ILookup<bool, GroupListRow> rowsByIsFinished = memberGroups.ToLookup(
                group => Group.EffectiveStatusOf(group.Status, group.ClosingStartedAt, now) == GroupStatus.Finished,
                group => group.Row);
            List<DeletedGroupListRow> recentlyDeleted = await DeletedGroupListRowsOf(userId.Value).ToListAsync(cancellationToken);

            return Result.Ok(new GroupListResponse([.. rowsByIsFinished[false]], [.. rowsByIsFinished[true]], recentlyDeleted));
        }

        private IQueryable<GroupListDto> GroupListDtosOf(Guid userId)
        {
            IQueryable<GroupMember> currentMembers = _context.CurrentGroupMembers();

            return _context.GroupsWithCurrentMember(userId)
                .AsNoTracking()
                .Where(group => group.DeletedAt == null)
                .OrderByDescending(group => group.CreatedAt)
                .Select(group => new GroupListDto(
                    new GroupListRow(
                        group.Id,
                        group.Kind,
                        group.Name,
                        group.Emoji,
                        group.DefaultCurrency,
                        currentMembers.Count(member => member.GroupId == group.Id)),
                    group.Status,
                    group.ClosingStartedAt));
        }

        private IQueryable<DeletedGroupListRow> DeletedGroupListRowsOf(Guid ownerUserId)
        {
            IQueryable<GroupMember> currentMembers = _context.CurrentGroupMembers();
            DateTimeOffset oldestRestorableDeletion = _timeProvider.GetUtcNow().AddDays(-Group.DeletedGroupRestoreDays);

            return _context.Groups
                .AsNoTracking()
                .Where(group => group.OwnerUserId == ownerUserId && group.DeletedAt > oldestRestorableDeletion)
                .OrderByDescending(group => group.DeletedAt)
                .Select(group => new DeletedGroupListRow(
                    group.Id,
                    group.Name,
                    group.Emoji,
                    group.DefaultCurrency,
                    currentMembers.Count(member => member.GroupId == group.Id),
                    group.DeletedAt!.Value,
                    Group.RestorableUntil(group.DeletedAt.Value)));
        }
    }
}
