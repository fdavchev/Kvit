using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Auth
{
    public sealed class GoogleLogInCommandHandler(
        IGoogleTokenChecker _googleTokenChecker,
        IAccountService _accountService,
        IUnitOfWork _unitOfWork) : ICommandHandler<GoogleLogInCommand, MeResponse>
    {
        public async Task<Result<MeResponse>> Handle(GoogleLogInCommand command, CancellationToken cancellationToken)
        {
            Result<GoogleIdentity> identity = await _googleTokenChecker.CheckAsync(command.IdToken, cancellationToken);
            if (!identity.IsSuccess)
            {
                return Result.Unauthorized<MeResponse>(identity.Error, identity.ErrorCode);
            }

            Result[] checks = [AccountRules.ValidateGoogleEmailVerified(identity.Value.EmailVerified), AccountRules.ValidateTimeZone(command.TimeZone)];
            Result? failed = checks.FirstOrDefault(check => !check.IsSuccess);
            if (failed is not null)
            {
                return Result.Failure<MeResponse>(failed.Error, failed.ErrorCode);
            }

            await _unitOfWork.OpenTransactionAsync(cancellationToken);
            Result<MeResponse> checkedLogIn = await _accountService.CheckGoogleLogInAsync(identity.Value, command.TimeZone, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            if (checkedLogIn.IsSuccess)
            {
                await _accountService.SignInAsync(checkedLogIn.Value.Id);
            }

            return checkedLogIn;
        }
    }
}
