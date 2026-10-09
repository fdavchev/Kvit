using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Domain.Services.ExchangeRates;
using Kvit.Domain.Services.Expenses;

namespace Kvit.Application.Commands.Expenses
{
    public sealed class EditExpenseCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        IExpenseRepository _expenses,
        GetCurrentExchangeRate _getCurrentExchangeRate,
        EditExpense _editExpense,
        IUnitOfWork _unitOfWork) : ICommandHandler<EditExpenseCommand>
    {
        public async Task<Result> Handle(EditExpenseCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            ExchangeRateSnapshot? currentRate = null;
            Currency? storedCurrency = await _expenses.FindCurrencyAsync(command.GroupId, command.ExpenseId, cancellationToken);
            if (storedCurrency is Currency currency && !string.Equals(currency.ToString(), command.Input.Currency, StringComparison.Ordinal))
            {
                Result<ExchangeRateSnapshot> rate = await _getCurrentExchangeRate.Execute(cancellationToken);
                if (!rate.IsSuccess)
                {
                    return rate;
                }

                currentRate = rate.Value;
            }

            return await _unitOfWork.RunInTransactionAsync(
                () => _editExpense.Execute(command.GroupId, userId.Value, command.ExpenseId, command.Input, currentRate, cancellationToken),
                cancellationToken);
        }
    }
}
