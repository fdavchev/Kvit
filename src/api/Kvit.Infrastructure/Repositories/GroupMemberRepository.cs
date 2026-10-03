using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Infrastructure.Persistence;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class GroupMemberRepository(AppDbContext _context) : IGroupMemberRepository
    {
        public void Add(GroupMember member)
        {
            _context.GroupMembers.Add(member);
        }
    }
}
