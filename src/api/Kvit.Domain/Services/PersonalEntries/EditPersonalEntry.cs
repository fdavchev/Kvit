using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Interfaces;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.PersonalEntries
{
    public sealed class EditPersonalEntry(
        IPersonalEntryRepository _entries,
        ICategoryRepository _categories,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid userId, Guid entryId, PersonalEntryInput input, ExchangeRateSnapshot? currentRate, CancellationToken cancellationToken)
        {
            Result<PersonalEntry> found = await _entries.FindOwnedAsync(userId, entryId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result<PersonalEntryFields> fields = PersonalEntryFields.Create(input, DateOnly.FromDateTime(now.UtcDateTime));
            if (!fields.IsSuccess)
            {
                return fields;
            }

            PersonalEntry entry = found.Value;
            if (fields.Value.CategoryId != entry.CategoryId)
            {
                Result category = await _categories.CheckPersonalEntryCategoryAsync(userId, fields.Value.CategoryId, cancellationToken);
                if (!category.IsSuccess)
                {
                    return category;
                }
            }

            if (fields.Value.Amount.Currency != entry.Currency)
            {
                ExchangeRateSnapshot rate = currentRate
                    ?? throw new InvalidOperationException($"Personal entry {entryId} changes its currency from {entry.Currency} to {fields.Value.Amount.Currency}, but no current exchange rate was read before the change. Its currency was changed by another request in the meantime.");
                entry.SaveRate(rate);
            }

            entry.Update(fields.Value, now);

            return Result.Ok();
        }
    }
}
