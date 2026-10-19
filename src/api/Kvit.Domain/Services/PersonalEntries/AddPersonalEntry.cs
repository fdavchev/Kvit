using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Interfaces;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.PersonalEntries
{
    public sealed class AddPersonalEntry(
        IPersonalEntryRepository _entries,
        ICategoryRepository _categories,
        TimeProvider _timeProvider)
    {
        public async Task<Result<Guid>> Execute(Guid userId, Guid clientRequestId, PersonalEntryInput input, ExchangeRateSnapshot rate, CancellationToken cancellationToken)
        {
            PersonalEntry? earlier = await _entries.FindByClientRequestIdAsync(clientRequestId, cancellationToken);
            if (earlier is not null)
            {
                return earlier.UserId == userId
                    ? Result.Ok(earlier.Id)
                    : PersonalEntry.ClientRequestIdUsed<Guid>(clientRequestId);
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result<PersonalEntryFields> fields = PersonalEntryFields.Create(input, DateOnly.FromDateTime(now.UtcDateTime));
            if (!fields.IsSuccess)
            {
                return fields.ToFailure<Guid>();
            }

            Result category = await _categories.CheckPersonalEntryCategoryAsync(userId, fields.Value.CategoryId, cancellationToken);
            if (!category.IsSuccess)
            {
                return category.ToFailure<Guid>();
            }

            PersonalEntry entry = PersonalEntry.CreateSpending(userId, fields.Value, rate, clientRequestId, now);
            _entries.Add(entry);

            return Result.Ok(entry.Id);
        }
    }
}
