using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Auth;
using Kvit.Domain.Accounts;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Me
{
    public sealed class SetBudgetTotalCurrencyCommandHandler(
        IDispatcher _dispatcher,
        IAccountService _accountService) : ICommandHandler<SetBudgetTotalCurrencyCommand>
    {
        public async Task<Result> Handle(SetBudgetTotalCurrencyCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId;
            }

            Result<Currency?> budgetTotalCurrency = AccountRules.ValidateBudgetTotalCurrency(command.BudgetTotalCurrency);
            if (!budgetTotalCurrency.IsSuccess)
            {
                return budgetTotalCurrency;
            }

            return await _accountService.SetBudgetTotalCurrencyAsync(userId.Value, budgetTotalCurrency.Value, cancellationToken);
        }
    }
}
