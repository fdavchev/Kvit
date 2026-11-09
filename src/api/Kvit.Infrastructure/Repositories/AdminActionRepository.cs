using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Infrastructure.Persistence;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class AdminActionRepository(AppDbContext _context) : IAdminActionRepository
    {
        public void Add(AdminAction adminAction)
        {
            _context.AdminActions.Add(adminAction);
        }
    }
}
