using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Auth
{
    public sealed class ChangePasswordCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        IAccountService _accountService,
        IUnitOfWork _unitOfWork) : ICommandHandler<ChangePasswordCommand>
    {
        public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            Result newPassword = AccountRules.ValidateNewPassword(command.CurrentPassword, command.NewPassword);
            if (!newPassword.IsSuccess)
            {
                return newPassword;
            }

            Result changed = await _unitOfWork.RunInTransactionKeepingRefusalsOnceMoreIfAccountChangedMeanwhileAsync(
                () => _accountService.ChangePasswordAsync(userId.Value, command.CurrentPassword, command.NewPassword, cancellationToken),
                cancellationToken);

            if (changed.IsSuccess)
            {
                await _accountService.SignInAsync(userId.Value);
            }

            return changed;
        }
    }
}
