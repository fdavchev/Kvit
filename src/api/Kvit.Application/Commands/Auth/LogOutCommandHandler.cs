using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Auth
{
    public sealed class LogOutCommandHandler(IAccountService _accountService) : ICommandHandler<LogOutCommand>
    {
        public async Task<Result> Handle(LogOutCommand command, CancellationToken cancellationToken)
        {
            await _accountService.LogOutAsync();

            return Result.Ok();
        }
    }
}
