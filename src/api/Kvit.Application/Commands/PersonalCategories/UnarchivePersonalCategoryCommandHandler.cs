using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Categories;

namespace Kvit.Application.Commands.PersonalCategories
{
    public sealed class UnarchivePersonalCategoryCommandHandler(
        IDispatcher _dispatcher,
        UnarchiveCategory _unarchiveCategory,
        IUnitOfWork _unitOfWork) : ICommandHandler<UnarchivePersonalCategoryCommand>
    {
        public async Task<Result> Handle(UnarchivePersonalCategoryCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionOrRefuseIfCategoryNameWasTakenAsync(
                () => _unarchiveCategory.Execute(userId.Value, command.CategoryId, cancellationToken),
                refused => refused,
                cancellationToken);
        }
    }
}
