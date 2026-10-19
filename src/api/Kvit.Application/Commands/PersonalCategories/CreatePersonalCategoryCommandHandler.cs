using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Application.Queries.PersonalCategories;
using Kvit.Contracts.PersonalCategories;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Categories;

namespace Kvit.Application.Commands.PersonalCategories
{
    public sealed class CreatePersonalCategoryCommandHandler(
        IDispatcher _dispatcher,
        CreateCategory _createCategory,
        IUnitOfWork _unitOfWork) : ICommandHandler<CreatePersonalCategoryCommand, PersonalCategoryRow>
    {
        public async Task<Result<PersonalCategoryRow>> Handle(CreatePersonalCategoryCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId.ToFailure<PersonalCategoryRow>();
            }

            Result<Guid> created = await _unitOfWork.RunInTransactionAsync(() => _createCategory.Execute(userId.Value, command.Name, command.Emoji, command.Color, cancellationToken), cancellationToken);
            if (!created.IsSuccess)
            {
                return created.ToFailure<PersonalCategoryRow>();
            }

            return await _dispatcher.Query<GetPersonalCategoryQuery, PersonalCategoryRow>(new GetPersonalCategoryQuery(created.Value), cancellationToken);
        }
    }
}
