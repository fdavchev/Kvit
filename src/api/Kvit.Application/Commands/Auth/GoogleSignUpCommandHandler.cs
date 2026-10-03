using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Auth
{
    public sealed class GoogleSignUpCommandHandler(
        IGoogleTokenChecker _googleTokenChecker,
        IAccountService _accountService,
        IUnitOfWork _unitOfWork) : ICommandHandler<GoogleSignUpCommand, MeResponse>
    {
        public async Task<Result<MeResponse>> Handle(GoogleSignUpCommand command, CancellationToken cancellationToken)
        {
            Result<GoogleIdentity> identity = await _googleTokenChecker.CheckAsync(command.IdToken, cancellationToken);
            if (!identity.IsSuccess)
            {
                return Result.Unauthorized<MeResponse>(identity.Error, identity.ErrorCode);
            }

            Result verified = AccountRules.ValidateGoogleEmailVerified(identity.Value.EmailVerified);
            if (!verified.IsSuccess)
            {
                return Result.Failure<MeResponse>(verified.Error, verified.ErrorCode);
            }

            Result<NewGoogleAccount> newAccount = NewGoogleAccount.Create(command.DisplayName, identity.Value.Email, command.TimeZone, command.Language);
            if (!newAccount.IsSuccess)
            {
                return Result.Failure<MeResponse>(newAccount.Error, newAccount.ErrorCode);
            }

            await _unitOfWork.OpenTransactionAsync(cancellationToken);
            Result<MeResponse> created = await _accountService.CreateGoogleAccountAsync(newAccount.Value, identity.Value.Subject, identity.Value.PictureUrl, cancellationToken);
            if (!created.IsSuccess)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return created;
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            await _accountService.SignInAsync(created.Value.Id);

            return created;
        }
    }
}
