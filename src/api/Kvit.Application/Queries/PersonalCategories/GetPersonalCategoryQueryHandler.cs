using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.PersonalCategories;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.PersonalCategories
{
    public sealed class GetPersonalCategoryQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetPersonalCategoryQuery, PersonalCategoryRow>
    {
        public async Task<Result<PersonalCategoryRow>> Handle(GetPersonalCategoryQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<PersonalCategoryRow>(userId.Error, userId.ErrorCode);
            }

            PersonalCategoryRow? row = await _context.Categories
                .AsNoTracking()
                .Where(category => category.Id == query.CategoryId && category.OwnerUserId == userId.Value)
                .PersonalCategoryRows()
                .SingleOrDefaultAsync(cancellationToken);
            if (row is null)
            {
                return Category.NotFound<PersonalCategoryRow>(query.CategoryId);
            }

            return Result.Ok(row);
        }
    }
}
