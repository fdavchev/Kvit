using Kvit.Domain.PersonalEntries;

namespace Kvit.Application.Commands.RecurringIncomes
{
    public sealed record CreateRecurringIncomeCommand(Guid ClientRequestId, RecurringIncomeInput Input);
}
