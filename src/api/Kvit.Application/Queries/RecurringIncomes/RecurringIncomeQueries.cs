using Kvit.Contracts.RecurringIncomes;
using Kvit.Domain.Entities;
using Kvit.Domain.RecurringIncomes;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.RecurringIncomes
{
    public static class RecurringIncomeQueries
    {
        public static IQueryable<RecurringIncome> RecurringIncomesOf(this AppDbContext context, Guid userId)
        {
            return context.RecurringIncomes
                .AsNoTracking()
                .Where(income => income.UserId == userId);
        }

        public static IQueryable<RecurringIncomeRow> RecurringIncomeRows(this IQueryable<RecurringIncome> incomes, IQueryable<PersonalEntry> entries, DateOnly userToday)
        {
            return incomes
                .OrderByDescending(income => income.CreatedAt)
                .ThenByDescending(income => income.Id)
                .Select(income => new RecurringIncomeRow(
                    income.Id,
                    income.AmountMinor,
                    income.Currency,
                    income.Title,
                    income.Frequency,
                    income.DayOfMonth,
                    income.DayOfWeek,
                    income.StartsOn,
                    income.EndsOn,
                    income.CreatedAt,
                    RecurringIncomeSchedule.NextDate(
                        income.Frequency,
                        income.DayOfMonth,
                        income.DayOfWeek,
                        income.StartsOn,
                        income.EndsOn,
                        entries.Where(entry => entry.RecurringIncomeId == income.Id).Max(entry => (DateOnly?)entry.EntryDate),
                        userToday)));
        }
    }
}
