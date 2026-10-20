using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Categories;

namespace Kvit.Application.Commands.PersonalCategories
{
    public sealed class ArchivePersonalCategoryCommandHandler(
        IDispatcher _dispatcher,
        ArchiveCategory _archiveCategory,
        IUnitOfWork _unitOfWork) : ICommandHandler<ArchivePersonalCategoryCommand>
    {
        public async Task<Result> Handle(ArchivePersonalCategoryCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _archiveCategory.Execute(userId.Value, command.CategoryId, cancellationToken), cancellationToken);
        }
    }
}
