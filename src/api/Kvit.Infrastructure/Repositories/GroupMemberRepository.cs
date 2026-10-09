using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class GroupMemberRepository(AppDbContext _context) : IGroupMemberRepository
    {
        public void Add(GroupMember member)
        {
            _context.GroupMembers.Add(member);
        }

        public async Task<GroupRoster> RosterOfAsync(Guid groupId, CancellationToken cancellationToken)
        {
            List<NamedMember> rows = await _context.NamedGroupMembers(groupId).ToListAsync(cancellationToken);
            HashSet<Guid> inAnyExpense = [.. await _context.MemberIdsInAnyExpenseOf(groupId).ToListAsync(cancellationToken)];

            return new GroupRoster([.. rows.Select(row => row with { IsInAnyExpense = inAnyExpense.Contains(row.Member.Id) })]);
        }

        public Task SaveAsync(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
