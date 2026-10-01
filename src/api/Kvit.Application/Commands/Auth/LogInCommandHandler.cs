using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Auth
{
    public sealed class LogInCommandHandler(
        IAccountService _accountService,
        IUnitOfWork _unitOfWork) : ICommandHandler<LogInCommand, MeResponse>
    {
        public async Task<Result<MeResponse>> Handle(LogInCommand command, CancellationToken cancellationToken)
        {
            Result timeZone = AccountRules.ValidateTimeZone(command.TimeZone);
            if (!timeZone.IsSuccess)
            {
                return Result.Failure<MeResponse>(timeZone.Error, timeZone.ErrorCode);
            }

            await _unitOfWork.OpenTransactionAsync(cancellationToken);
            Result<MeResponse> checkedLogIn = await _accountService.CheckLogInAsync(command.Email, command.Password, command.TimeZone, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            if (checkedLogIn.IsSuccess)
            {
                await _accountService.SignInAsync(checkedLogIn.Value.Id);
            }

            return checkedLogIn;
        }
    }
}
