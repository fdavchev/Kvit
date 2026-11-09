using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class GroupRepository(AppDbContext _context) : IGroupRepository
    {
        public void Add(Group group)
        {
            _context.Groups.Add(group);
        }

        public Task<Result<Group>> FindForMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken)
        {
            return SingleOrNotFoundAsync(_context.GroupsWithCurrentMember(userId).Where(group => group.DeletedAt == null), groupId, cancellationToken);
        }

        public Task<Result<Group>> FindIncludingDeletedForMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken)
        {
            return SingleOrNotFoundAsync(_context.GroupsWithCurrentMember(userId), groupId, cancellationToken);
        }

        public async Task<Result<Group>> FindByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken)
        {
            Group? group = await _context.Groups.SingleOrDefaultAsync(candidate => candidate.InviteToken == inviteToken && candidate.DeletedAt == null, cancellationToken);
            if (group is null)
            {
                return Group.InviteNotFound<Group>();
            }

            return Result.Ok(group);
        }

        public async Task<Result<Group>> LockAnyIncludingDeletedAsync(Guid groupId, CancellationToken cancellationToken)
        {
            List<Group> groups = await _context.Groups
                .FromSql($"SELECT * FROM groups WHERE id = {groupId} FOR UPDATE")
                .ToListAsync(cancellationToken);
            if (groups.SingleOrDefault() is not Group group)
            {
                return Group.NotFoundAnywhere<Group>(groupId);
            }

            return Result.Ok(group);
        }

        private static async Task<Result<Group>> SingleOrNotFoundAsync(IQueryable<Group> groups, Guid groupId, CancellationToken cancellationToken)
        {
            Group? group = await groups.SingleOrDefaultAsync(candidate => candidate.Id == groupId, cancellationToken);
            if (group is null)
            {
                return Group.NotFound<Group>(groupId);
            }

            return Result.Ok(group);
        }
    }
}
