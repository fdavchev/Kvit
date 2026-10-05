using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
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

            Result<MeResponse> created = await _unitOfWork.RunInTransactionAsync(() => _accountService.CreateAccountAsync(newAccount.Value, cancellationToken), cancellationToken);
            if (!created.IsSuccess)
            {
                return created;
            }

            await _accountService.SignInAsync(created.Value.Id);

            return created;
        }
    }
}
