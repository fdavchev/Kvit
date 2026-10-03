using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Auth
{
    public sealed class SetPasswordCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        IAccountService _accountService,
        IUnitOfWork _unitOfWork) : ICommandHandler<SetPasswordCommand>
    {
        public async Task<Result> Handle(SetPasswordCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            Result newPassword = AccountRules.ValidatePassword(command.NewPassword);
            if (!newPassword.IsSuccess)
            {
                return newPassword;
            }

            Result set = await _unitOfWork.RunInTransactionAsync(() => _accountService.SetPasswordAsync(userId.Value, command.NewPassword, cancellationToken), cancellationToken);
            if (!set.IsSuccess)
            {
                return set;
            }

            await _accountService.SignInAsync(userId.Value);

            return set;
        }
    }
}
