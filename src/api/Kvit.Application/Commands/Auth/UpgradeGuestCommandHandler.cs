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
    public sealed class UpgradeGuestCommandHandler(
        IDispatcher _dispatcher,
        IAccountService _accountService,
        IUnitOfWork _unitOfWork) : ICommandHandler<UpgradeGuestCommand, MeResponse>
    {
        public async Task<Result<MeResponse>> Handle(UpgradeGuestCommand command, CancellationToken cancellationToken)
        {
            Result<MeResponse> me = await _dispatcher.Query<GetMeQuery, MeResponse>(new GetMeQuery(), cancellationToken);
            if (!me.IsSuccess)
            {
                return me;
            }

            Result isGuest = AccountRules.CheckIsGuest(me.Value.IsGuest);
            if (!isGuest.IsSuccess)
            {
                return isGuest.ToFailure<MeResponse>();
            }

            Result<string> email = AccountRules.ValidateEmail(command.Email);
            if (!email.IsSuccess)
            {
                return email.ToFailure<MeResponse>();
            }

            Result password = AccountRules.ValidatePassword(command.Password);
            if (!password.IsSuccess)
            {
                return password.ToFailure<MeResponse>();
            }

            Guid userId = me.Value.Id;
            Result<MeResponse> upgraded = await _unitOfWork.RunInTransactionAsync(() => _accountService.UpgradeGuestAsync(userId, email.Value, command.Password, cancellationToken), cancellationToken);
            if (!upgraded.IsSuccess)
            {
                return upgraded;
            }

            await _accountService.SignInAsync(userId);

            return upgraded;
        }
    }
}
