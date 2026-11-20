using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Me
{
    public sealed class DeleteAccountCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        IAccountService _accountService,
        IUnitOfWork _unitOfWork) : ICommandHandler<DeleteAccountCommand>
    {
        public async Task<Result> Handle(DeleteAccountCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            Result<DeletionNameChoice> nameChoice = AccountDeletionRules.ValidateNameChoice(command.NameChoice);
            if (!nameChoice.IsSuccess)
            {
                return nameChoice;
            }

            return await _unitOfWork.RunInTransactionOnceMoreIfAccountChangedMeanwhileAsync(
                () => _accountService.RequestDeletionAsync(userId.Value, nameChoice.Value, cancellationToken),
                cancellationToken);
        }
    }
}
