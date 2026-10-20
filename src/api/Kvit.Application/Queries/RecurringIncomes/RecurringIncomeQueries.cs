using Kvit.Contracts.RecurringIncomes;
using Kvit.Domain.Entities;
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

        public static IQueryable<RecurringIncomeRow> RecurringIncomeRows(this IQueryable<RecurringIncome> incomes)
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
                    income.CreatedAt));
        }
    }
}
