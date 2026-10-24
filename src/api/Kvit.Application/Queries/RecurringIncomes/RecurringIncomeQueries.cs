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
                .Select(income => new
                {
                    Income = income,
                    LastEntryDate = entries.Where(entry => entry.RecurringIncomeId == income.Id).Max(entry => (DateOnly?)entry.EntryDate),
                })
                .Select(row => new RecurringIncomeRow(
                    row.Income.Id,
                    row.Income.AmountMinor,
                    row.Income.Currency,
                    row.Income.Title,
                    row.Income.Frequency,
                    row.Income.DayOfMonth,
                    row.Income.DayOfWeek,
                    row.Income.StartsOn,
                    row.Income.EndsOn,
                    row.Income.CreatedAt,
                    RecurringIncomeSchedule.NextDate(
                        row.Income.Frequency,
                        row.Income.DayOfMonth,
                        row.Income.DayOfWeek,
                        row.Income.StartsOn,
                        row.Income.EndsOn,
                        row.LastEntryDate,
                        userToday),
                    row.LastEntryDate != null));
        }
    }
}
