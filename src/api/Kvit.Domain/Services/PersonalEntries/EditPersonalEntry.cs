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
        public Task<Result> Execute(Guid userId, Guid entryId, PersonalEntryInput input, CancellationToken cancellationToken)
        {
            return EditAsync(
                userId,
                entryId,
                PersonalEntryKind.Spending,
                (entry, utcToday) => CheckSpendingAsync(userId, entry, input, utcToday, cancellationToken),
                (entry, fields, now) => entry.Update(fields, now),
                cancellationToken);
        }

        public Task<Result> Execute(Guid userId, Guid entryId, IncomeInput input, CancellationToken cancellationToken)
        {
            return EditAsync(
                userId,
                entryId,
                PersonalEntryKind.Income,
                (entry, utcToday) => Task.FromResult(IncomeFields.CreateForEdit(input, utcToday, entry.EntryDate)),
                (entry, fields, now) => entry.Update(fields, now),
                cancellationToken);
        }

        private async Task<Result> EditAsync<TFields>(
            Guid userId,
            Guid entryId,
            PersonalEntryKind kind,
            Func<PersonalEntry, DateOnly, Task<Result<TFields>>> checkFields,
            Action<PersonalEntry, TFields, DateTimeOffset> update,
            CancellationToken cancellationToken)
            where TFields : EntryAmountDateAndNote
        {
            Result<PersonalEntry> found = await FindOwnedOfKindAsync(userId, entryId, kind, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            PersonalEntry entry = found.Value;
            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result<TFields> fields = await checkFields(entry, DateOnly.FromDateTime(now.UtcDateTime));
            if (!fields.IsSuccess)
            {
                return fields;
            }

            Result dateCanChange = entry.CheckDateCanChangeTo(fields.Value.EntryDate);
            if (!dateCanChange.IsSuccess)
            {
                return dateCanChange;
            }

            Result rate = await SaveCurrentRateIfTheCurrencyChangesAsync(entry, fields.Value.Amount.Currency, cancellationToken);
            if (!rate.IsSuccess)
            {
                return rate;
            }

            update(entry, fields.Value, now);

            return Result.Ok();
        }

        private async Task<Result<PersonalEntryFields>> CheckSpendingAsync(Guid userId, PersonalEntry entry, PersonalEntryInput input, DateOnly utcToday, CancellationToken cancellationToken)
        {
            Result<PersonalEntryFields> fields = PersonalEntryFields.CreateForEdit(input, utcToday, entry.EntryDate);
            if (!fields.IsSuccess)
            {
                return fields;
            }

            if (fields.Value.CategoryId != entry.CategoryId)
            {
                Result category = await _categories.CheckPersonalEntryCategoryAsync(userId, fields.Value.CategoryId, cancellationToken);
                if (!category.IsSuccess)
                {
                    return category.ToFailure<PersonalEntryFields>();
                }
            }

            return fields;
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
