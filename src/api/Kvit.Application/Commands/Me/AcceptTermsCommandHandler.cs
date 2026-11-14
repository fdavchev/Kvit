using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Me
{
    public sealed class AcceptTermsCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        IAccountService _accountService) : ICommandHandler<AcceptTermsCommand>
    {
        public async Task<Result> Handle(AcceptTermsCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _accountService.AcceptCurrentTermsAsync(userId.Value, cancellationToken);
        }
    }
}
