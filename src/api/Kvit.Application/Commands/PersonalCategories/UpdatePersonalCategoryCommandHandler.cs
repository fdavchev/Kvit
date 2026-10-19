using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Categories;

namespace Kvit.Application.Commands.PersonalCategories
{
    public sealed class UpdatePersonalCategoryCommandHandler(
        IDispatcher _dispatcher,
        UpdateCategory _updateCategory,
        IUnitOfWork _unitOfWork) : ICommandHandler<UpdatePersonalCategoryCommand>
    {
        public async Task<Result> Handle(UpdatePersonalCategoryCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _updateCategory.Execute(userId.Value, command.CategoryId, command.Name, command.Emoji, command.Color, cancellationToken), cancellationToken);
        }
    }
}
