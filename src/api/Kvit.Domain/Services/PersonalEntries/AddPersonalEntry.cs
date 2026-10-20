using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Interfaces;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;
using Kvit.Domain.Services.ExchangeRates;

namespace Kvit.Domain.Services.PersonalEntries
{
    public sealed class AddPersonalEntry(
        IPersonalEntryRepository _entries,
        ICategoryRepository _categories,
        GetCurrentExchangeRate _getCurrentExchangeRate,
        TimeProvider _timeProvider)
    {
        public async Task<Result<Guid>> Execute(Guid userId, Guid clientRequestId, PersonalEntryInput input, CancellationToken cancellationToken)
        {
            PersonalEntry? earlier = await _entries.FindByClientRequestIdAsync(clientRequestId, cancellationToken);
            if (earlier is not null)
            {
                return AnswerForEarlier(earlier, userId, clientRequestId);
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

            Result<ExchangeRateSnapshot> rate = await _getCurrentExchangeRate.Execute(cancellationToken);
            if (!rate.IsSuccess)
            {
                return rate.ToFailure<Guid>();
            }

            PersonalEntry entry = PersonalEntry.CreateSpending(userId, fields.Value, rate.Value, clientRequestId, now);
            _entries.Add(entry);

            return Result.Ok(entry.Id);
        }

        private static Result<Guid> AnswerForEarlier(PersonalEntry earlier, Guid userId, Guid clientRequestId)
        {
            if (earlier.UserId != userId)
            {
                return PersonalEntry.ClientRequestIdUsed<Guid>(clientRequestId);
            }

            if (earlier.DeletedAt is not null)
            {
                return PersonalEntry.ClientRequestIdUsedByDeletedEntry<Guid>(clientRequestId, earlier.Id);
            }

            return Result.Ok(earlier.Id);
        }
    }
}
