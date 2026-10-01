using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Me
{
    public sealed class ChangeLanguageCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        IAccountService _accountService) : ICommandHandler<ChangeLanguageCommand>
    {
        public async Task<Result> Handle(ChangeLanguageCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            Result language = AccountRules.ValidateLanguage(command.Language);
            if (!language.IsSuccess)
            {
                return language;
            }

            return await _accountService.ChangeLanguageAsync(userId.Value, command.Language, cancellationToken);
        }
    }
}
