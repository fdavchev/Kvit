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
        public Task<Result<Guid>> Execute(Guid userId, Guid clientRequestId, PersonalEntryInput input, CancellationToken cancellationToken)
        {
            return AddAsync(
                userId,
                clientRequestId,
                utcToday => CheckSpendingAsync(userId, input, utcToday, cancellationToken),
                (fields, rate, now) => PersonalEntry.CreateSpending(userId, fields, rate, clientRequestId, now),
                cancellationToken);
        }

        public Task<Result<Guid>> Execute(Guid userId, Guid clientRequestId, IncomeInput input, CancellationToken cancellationToken)
        {
            return AddAsync(
                userId,
                clientRequestId,
                utcToday => Task.FromResult(IncomeFields.Create(input, utcToday)),
                (fields, rate, now) => PersonalEntry.CreateIncome(userId, fields, rate, clientRequestId, now),
                cancellationToken);
        }

        private async Task<Result<Guid>> AddAsync<TFields>(
            Guid userId,
            Guid clientRequestId,
            Func<DateOnly, Task<Result<TFields>>> checkFields,
            Func<TFields, ExchangeRateSnapshot, DateTimeOffset, PersonalEntry> createEntry,
            CancellationToken cancellationToken)
        {
            PersonalEntry? earlier = await _entries.FindByClientRequestIdAsync(clientRequestId, cancellationToken);
            if (earlier is not null)
            {
                return earlier.AnswerToRepeatBy(userId);
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result<TFields> fields = await checkFields(DateOnly.FromDateTime(now.UtcDateTime));
            if (!fields.IsSuccess)
            {
                return fields.ToFailure<Guid>();
            }

            Result<ExchangeRateSnapshot> rate = await _getCurrentExchangeRate.Execute(cancellationToken);
            if (!rate.IsSuccess)
            {
                return rate.ToFailure<Guid>();
            }

            PersonalEntry entry = createEntry(fields.Value, rate.Value, now);
            _entries.Add(entry);

            return Result.Ok(entry.Id);
        }

        private async Task<Result<PersonalEntryFields>> CheckSpendingAsync(Guid userId, PersonalEntryInput input, DateOnly utcToday, CancellationToken cancellationToken)
        {
            Result<PersonalEntryFields> fields = PersonalEntryFields.Create(input, utcToday);
            if (!fields.IsSuccess)
            {
                return fields;
            }

            Result category = await _categories.CheckPersonalEntryCategoryAsync(userId, fields.Value.CategoryId, cancellationToken);
            if (!category.IsSuccess)
            {
                return category.ToFailure<PersonalEntryFields>();
            }

            return fields;
        }
    }
}
