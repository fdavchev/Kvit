using Kvit.Domain.PersonalEntries;

namespace Kvit.Application.Commands.RecurringIncomes
{
    public sealed record EditRecurringIncomeCommand(Guid RecurringIncomeId, RecurringIncomeInput Input);
}
