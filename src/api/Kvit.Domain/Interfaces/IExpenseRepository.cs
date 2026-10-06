using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Interfaces
{
    public interface IExpenseRepository
    {
        void Add(Expense expense);

        Task<Expense?> FindByClientRequestIdAsync(Guid clientRequestId, CancellationToken cancellationToken);

        Task<Result<Expense>> FindInGroupAsync(Guid groupId, Guid expenseId, CancellationToken cancellationToken);

        Task<Result<Expense>> FindIncludingDeletedInGroupAsync(Guid groupId, Guid expenseId, CancellationToken cancellationToken);

        Task<Currency?> FindCurrencyAsync(Guid groupId, Guid expenseId, CancellationToken cancellationToken);
    }
}
