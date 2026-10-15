using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class ClosingResponseRepository(AppDbContext _context) : IClosingResponseRepository
    {
        public void Add(ClosingResponse response)
        {
            _context.ClosingResponses.Add(response);
        }

        public async Task<IReadOnlyList<ClosingResponse>> ResponsesOfAsync(Guid groupId, CancellationToken cancellationToken)
        {
            return await _context.ClosingResponses
                .Where(response => response.GroupId == groupId)
                .ToListAsync(cancellationToken);
        }

        public async Task RemoveAllOfAsync(Guid groupId, CancellationToken cancellationToken)
        {
            IReadOnlyList<ClosingResponse> responses = await ResponsesOfAsync(groupId, cancellationToken);
            _context.ClosingResponses.RemoveRange(responses);
        }
    }
}
