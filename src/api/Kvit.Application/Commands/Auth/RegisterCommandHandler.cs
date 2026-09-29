using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Auth
{
    public sealed class RegisterCommandHandler(
        IAccountService _accountService,
        IUnitOfWork _unitOfWork) : ICommandHandler<RegisterCommand, MeResponse>
    {
        public async Task<Result<MeResponse>> Handle(RegisterCommand command, CancellationToken cancellationToken)
        {
            Result<NewAccount> newAccount = NewAccount.Create(command.DisplayName, command.Email, command.Password, command.TimeZone, command.Language);
            if (!newAccount.IsSuccess)
            {
                return Result.Failure<MeResponse>(newAccount.Error, newAccount.ErrorCode);
            }

            await _unitOfWork.OpenTransactionAsync(cancellationToken);
            Result<MeResponse> created = await _accountService.CreateAccountAsync(newAccount.Value, cancellationToken);
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
