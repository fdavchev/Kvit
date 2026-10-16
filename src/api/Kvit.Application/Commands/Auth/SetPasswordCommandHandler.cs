using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Auth
{
    public sealed class SetPasswordCommandHandler(
        IDispatcher _dispatcher,
        IAccountService _accountService,
        IUnitOfWork _unitOfWork) : ICommandHandler<SetPasswordCommand>
    {
        public async Task<Result> Handle(SetPasswordCommand command, CancellationToken cancellationToken)
        {
            Result<MeResponse> me = await _dispatcher.Query<GetMeQuery, MeResponse>(new GetMeQuery(), cancellationToken);
            if (!me.IsSuccess)
            {
                return me;
            }

            Result notGuest = AccountRules.CheckIsNotGuest(me.Value.IsGuest);
            if (!notGuest.IsSuccess)
            {
                return notGuest;
            }

            Result newPassword = AccountRules.ValidatePassword(command.NewPassword);
            if (!newPassword.IsSuccess)
            {
                return newPassword;
            }

            Guid userId = me.Value.Id;
            Result set = await _unitOfWork.RunInTransactionAsync(() => _accountService.SetPasswordAsync(userId, command.NewPassword, cancellationToken), cancellationToken);
            if (!set.IsSuccess)
            {
                return set;
            }

            await _accountService.SignInAsync(userId);

            return set;
        }
    }
}
