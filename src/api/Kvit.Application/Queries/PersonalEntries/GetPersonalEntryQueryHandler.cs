using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.PersonalCategories;
using Kvit.Contracts.PersonalEntries;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.PersonalEntries
{
    public sealed class GetPersonalEntryQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetPersonalEntryQuery, PersonalEntryRow>
    {
        public async Task<Result<PersonalEntryRow>> Handle(GetPersonalEntryQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<PersonalEntryRow>(userId.Error, userId.ErrorCode);
            }

            PersonalEntryDto? entry = await _context.EntriesOf(userId.Value)
                .Where(candidate => candidate.Id == query.EntryId)
                .PersonalEntryDtos()
                .SingleOrDefaultAsync(cancellationToken);
            if (entry is null)
            {
                return PersonalEntry.NotFound<PersonalEntryRow>(query.EntryId);
            }

            List<PersonalCategoryRow> categories = await _context.PersonalCategoryRowsOfAsync([entry], cancellationToken);

            return Result.Ok(PersonalEntryQueries.PersonalEntryRowOf(entry, categories.ToDictionary(category => category.Id)));
        }
    }
}
