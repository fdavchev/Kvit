using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.PersonalCategories;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.PersonalCategories
{
    public sealed class GetPersonalCategoriesQueryHandler(
        IDispatcher _dispatcher,
        AppDbContext _context) : IQueryHandler<GetPersonalCategoriesQuery, PersonalCategoryListResponse>
    {
        public async Task<Result<PersonalCategoryListResponse>> Handle(GetPersonalCategoriesQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId.ToFailure<PersonalCategoryListResponse>();
            }

            List<PersonalCategoryRow> categories = await VisibleCategories(userId.Value, query.IncludeArchived).PersonalCategoryRows().ToListAsync(cancellationToken);

            return Result.Ok(new PersonalCategoryListResponse(categories));
        }

        private IQueryable<Category> VisibleCategories(Guid userId, bool includeArchived)
        {
            IQueryable<Category> builtInsAndOwn = _context.Categories
                .AsNoTracking()
                .Where(category => category.OwnerUserId == null || category.OwnerUserId == userId);

            return includeArchived ? builtInsAndOwn : builtInsAndOwn.Where(category => category.ArchivedAt == null);
        }
    }
}
