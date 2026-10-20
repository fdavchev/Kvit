using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;
using Kvit.Domain.Services.ExchangeRates;

namespace Kvit.Domain.Services.PersonalEntries
{
    public sealed class EditPersonalEntry(
        IPersonalEntryRepository _entries,
        ICategoryRepository _categories,
        GetCurrentExchangeRate _getCurrentExchangeRate,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid userId, Guid entryId, PersonalEntryInput input, CancellationToken cancellationToken)
        {
            Result<PersonalEntry> found = await FindOwnedOfKindAsync(userId, entryId, PersonalEntryKind.Spending, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            PersonalEntry entry = found.Value;
            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result<PersonalEntryFields> fields = PersonalEntryFields.CreateForEdit(input, DateOnly.FromDateTime(now.UtcDateTime), entry.EntryDate);
            if (!fields.IsSuccess)
            {
                return fields;
            }

            if (fields.Value.CategoryId != entry.CategoryId)
            {
                Result category = await _categories.CheckPersonalEntryCategoryAsync(userId, fields.Value.CategoryId, cancellationToken);
                if (!category.IsSuccess)
                {
                    return category;
                }
            }

            Result rate = await SaveCurrentRateIfTheCurrencyChangesAsync(entry, fields.Value.Amount.Currency, cancellationToken);
            if (!rate.IsSuccess)
            {
                return rate;
            }

            entry.Update(fields.Value, now);

            return Result.Ok();
        }

        public async Task<Result> Execute(Guid userId, Guid entryId, IncomeInput input, CancellationToken cancellationToken)
        {
            Result<PersonalEntry> found = await FindOwnedOfKindAsync(userId, entryId, PersonalEntryKind.Income, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            PersonalEntry entry = found.Value;
            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result<IncomeFields> fields = IncomeFields.CreateForEdit(input, DateOnly.FromDateTime(now.UtcDateTime), entry.EntryDate);
            if (!fields.IsSuccess)
            {
                return fields;
            }

            Result rate = await SaveCurrentRateIfTheCurrencyChangesAsync(entry, fields.Value.Amount.Currency, cancellationToken);
            if (!rate.IsSuccess)
            {
                return rate;
            }

            entry.Update(fields.Value, now);

            return Result.Ok();
        }

        private async Task<Result<PersonalEntry>> FindOwnedOfKindAsync(Guid userId, Guid entryId, PersonalEntryKind kind, CancellationToken cancellationToken)
        {
            Result<PersonalEntry> found = await _entries.FindOwnedAsync(userId, entryId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            Result sameKind = found.Value.CheckKindIs(kind);
            if (!sameKind.IsSuccess)
            {
                return sameKind.ToFailure<PersonalEntry>();
            }

            return found;
        }

        private async Task<Result> SaveCurrentRateIfTheCurrencyChangesAsync(PersonalEntry entry, Currency newCurrency, CancellationToken cancellationToken)
        {
            if (newCurrency == entry.Currency)
            {
                return Result.Ok();
            }

            Result<ExchangeRateSnapshot> rate = await _getCurrentExchangeRate.Execute(cancellationToken);
            if (!rate.IsSuccess)
            {
                return rate;
            }

            entry.SaveRate(rate.Value);

            return Result.Ok();
        }
    }
}
