using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Groups;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Groups
{
    public sealed class GetGroupMembersQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetGroupMembersQuery, GroupMembersResponse>
    {
        public async Task<Result<GroupMembersResponse>> Handle(GetGroupMembersQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<GroupMembersResponse>(userId.Error, userId.ErrorCode);
            }

            Guid? ownerUserId = await _context.GroupsWithCurrentMember(userId.Value)
                .AsNoTracking()
                .Where(group => group.Id == query.GroupId && group.DeletedAt == null)
                .Select(group => (Guid?)group.OwnerUserId)
                .SingleOrDefaultAsync(cancellationToken);
            if (ownerUserId is not Guid owner)
            {
                return Group.NotFound<GroupMembersResponse>(query.GroupId);
            }

            List<GroupMemberRow> members = await GroupMemberRowsOf(query.GroupId, userId.Value, owner).ToListAsync(cancellationToken);
            List<RemovedMemberRow> removed = owner == userId.Value
                ? await RemovedMemberRowsOf(query.GroupId).ToListAsync(cancellationToken)
                : [];
            GroupMemberRow ownRow = members.Single(member => member.IsYou);
            bool isOwnRowInAnyExpense = await _context.MemberIdsInAnyExpenseOf(query.GroupId).ContainsAsync(ownRow.Id, cancellationToken);
            bool canClaimNames = ownRow.ClaimedName is null && !isOwnRowInAnyExpense;

            return Result.Ok(new GroupMembersResponse(members, removed, canClaimNames));
        }

        private IQueryable<GroupMemberRow> GroupMemberRowsOf(Guid groupId, Guid userId, Guid ownerUserId)
        {
            return _context.NamedGroupMembers(groupId)
                .AsNoTracking()
                .Where(named => named.Member.RemovedAt == null)
                .InJoiningOrder()
                .Select(named => new GroupMemberRow(
                    named.Member.Id,
                    named.Member.Name,
                    named.DisplayName,
                    named.Member.UserId,
                    named.Member.UserId == ownerUserId,
                    named.Member.UserId == userId,
                    named.Member.UserId == null,
                    named.Member.ClaimedAt == null ? null : named.Member.Name));
        }

        private IQueryable<RemovedMemberRow> RemovedMemberRowsOf(Guid groupId)
        {
            return _context.NamedGroupMembers(groupId)
                .AsNoTracking()
                .Where(named => named.Member.EndKind == MemberEndKind.Removed)
                .InJoiningOrder()
                .Select(named => new RemovedMemberRow(named.Member.Id, named.DisplayName));
        }
    }
}
