using Kvit.Domain.ExchangeRates;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public class PersonalEntry
    {
        private PersonalEntry()
        {
        }

        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public PersonalEntryKind Kind { get; private set; }

        public long AmountMinor { get; private set; }

        public Currency Currency { get; private set; }

        public decimal MkdPerEur { get; private set; }

        public DateOnly RateDate { get; private set; }

        public DateOnly EntryDate { get; private set; }

        public Guid? CategoryId { get; private set; }

        public string? Note { get; private set; }

        public Guid ClientRequestId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset? UpdatedAt { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public static PersonalEntry CreateSpending(Guid userId, PersonalEntryFields fields, ExchangeRateSnapshot rate, Guid clientRequestId, DateTimeOffset createdAt)
        {
            PersonalEntry entry = new()
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                Kind = PersonalEntryKind.Spending,
                ClientRequestId = clientRequestId,
                CreatedAt = createdAt,
            };
            entry.ApplyFields(fields);
            entry.SaveRate(rate);

            return entry;
        }

        public static Result<T> NotFound<T>(Guid entryId)
        {
            return Result.NotFound<T>($"Personal entry {entryId} is not one of your entries, or it was deleted.", ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }

        public static Result<T> ClientRequestIdUsed<T>(Guid clientRequestId)
        {
            return Result.Failure<T>($"The request id {clientRequestId} was already used by another person.", ResultCodes.PERSONAL_ENTRY_CLIENT_REQUEST_ID_USED);
        }

        public static DateTimeOffset RestorableUntil(DateTimeOffset deletedAt)
        {
            return Expense.RestorableUntil(deletedAt);
        }

        public void Update(PersonalEntryFields fields, DateTimeOffset updatedAt)
        {
            ApplyFields(fields);
            UpdatedAt = updatedAt;
        }

        public void SaveRate(ExchangeRateSnapshot rate)
        {
            MkdPerEur = rate.MkdPerEur;
            RateDate = rate.RateDate;
        }

        public void Delete(DateTimeOffset deletedAt)
        {
            DeletedAt = deletedAt;
        }

        public Result Restore(DateTimeOffset now)
        {
            if (DeletedAt is not DateTimeOffset deletedAt)
            {
                return Result.Failure($"Personal entry {Id} is not deleted.", ResultCodes.PERSONAL_ENTRY_NOT_DELETED);
            }

            if (now >= RestorableUntil(deletedAt))
            {
                return Result.Failure($"Personal entry {Id} was deleted more than {Expense.DeletedExpenseRestoreDays} days ago and can no longer be restored.", ResultCodes.PERSONAL_ENTRY_RESTORE_EXPIRED);
            }

            DeletedAt = null;

            return Result.Ok();
        }

        private void ApplyFields(PersonalEntryFields fields)
        {
            CategoryId = fields.CategoryId;
            AmountMinor = fields.Amount.MinorUnits;
            Currency = fields.Amount.Currency;
            EntryDate = fields.EntryDate;
            Note = fields.Note;
        }
    }
}
