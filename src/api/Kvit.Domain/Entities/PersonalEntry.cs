using System.Globalization;
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

        public Guid? RecurringIncomeId { get; private set; }

        public Guid? ClientRequestId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset? UpdatedAt { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public static PersonalEntry CreateSpending(Guid userId, PersonalEntryFields fields, ExchangeRateSnapshot rate, Guid clientRequestId, DateTimeOffset createdAt)
        {
            PersonalEntry entry = New(Guid.CreateVersion7(), userId, PersonalEntryKind.Spending, rate, clientRequestId, createdAt);
            entry.ApplyFields(fields);

            return entry;
        }

        public static PersonalEntry CreateIncome(Guid userId, IncomeFields fields, ExchangeRateSnapshot rate, Guid clientRequestId, DateTimeOffset createdAt)
        {
            PersonalEntry entry = New(Guid.CreateVersion7(), userId, PersonalEntryKind.Income, rate, clientRequestId, createdAt);
            entry.ApplyAmountDateAndNote(fields);

            return entry;
        }

        public static PersonalEntry CreateFromRecurringIncome(Guid id, RecurringIncome income, DateOnly entryDate, ExchangeRateSnapshot rate, DateTimeOffset createdAt)
        {
            PersonalEntry entry = New(id, income.UserId, PersonalEntryKind.Income, rate, null, createdAt);
            entry.RecurringIncomeId = income.Id;
            entry.AmountMinor = income.AmountMinor;
            entry.Currency = income.Currency;
            entry.EntryDate = entryDate;
            entry.Note = income.Title;

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

        public static Result<T> ClientRequestIdUsedByDeletedEntry<T>(Guid clientRequestId, Guid deletedEntryId)
        {
            return Result.Failure<T>($"The request id {clientRequestId} already saved personal entry {deletedEntryId}, which was deleted since; nothing was saved again.", ResultCodes.PERSONAL_ENTRY_CLIENT_REQUEST_ID_USED);
        }

        public static DateTimeOffset RestorableUntil(DateTimeOffset deletedAt)
        {
            return Expense.RestorableUntil(deletedAt);
        }

        public Result<Guid> AnswerToRepeatBy(Guid userId)
        {
            Guid clientRequestId = ClientRequestId ?? throw new InvalidOperationException($"Personal entry {Id} was created from a repeating income and has no request id, so it cannot answer a repeated request.");
            if (UserId != userId)
            {
                return ClientRequestIdUsed<Guid>(clientRequestId);
            }

            if (DeletedAt is not null)
            {
                return ClientRequestIdUsedByDeletedEntry<Guid>(clientRequestId, Id);
            }

            return Result.Ok(Id);
        }

        public Result CheckKindIs(PersonalEntryKind kind)
        {
            if (Kind != kind)
            {
                return Result.Failure($"Personal entry {Id} is {Kind} and cannot be changed to {kind}.", ResultCodes.PERSONAL_ENTRY_KIND_CANNOT_CHANGE);
            }

            return Result.Ok();
        }

        public Result CheckDateCanChangeTo(DateOnly entryDate)
        {
            if (RecurringIncomeId is Guid recurringIncomeId && entryDate != EntryDate)
            {
                return Result.Failure($"Personal entry {Id} was created by repeating income {recurringIncomeId}, so its date {EntryDate.ToString(ExpenseFields.DateFormat, CultureInfo.InvariantCulture)} cannot change to {entryDate.ToString(ExpenseFields.DateFormat, CultureInfo.InvariantCulture)}.", ResultCodes.PERSONAL_ENTRY_DATE_LOCKED);
            }

            return Result.Ok();
        }

        public DuplicateMatchFields FieldsDuplicatesMatchOn()
        {
            return new DuplicateMatchFields(AmountMinor, Currency, CategoryId, EntryDate);
        }

        public void Update(PersonalEntryFields fields, DateTimeOffset updatedAt)
        {
            ThrowUnlessKindIs(PersonalEntryKind.Spending);
            ApplyFields(fields);
            UpdatedAt = updatedAt;
        }

        public void Update(IncomeFields fields, DateTimeOffset updatedAt)
        {
            ThrowUnlessKindIs(PersonalEntryKind.Income);
            ApplyAmountDateAndNote(fields);
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

        private static PersonalEntry New(Guid id, Guid userId, PersonalEntryKind kind, ExchangeRateSnapshot rate, Guid? clientRequestId, DateTimeOffset createdAt)
        {
            PersonalEntry entry = new()
            {
                Id = id,
                UserId = userId,
                Kind = kind,
                ClientRequestId = clientRequestId,
                CreatedAt = createdAt,
            };
            entry.SaveRate(rate);

            return entry;
        }

        private void ThrowUnlessKindIs(PersonalEntryKind kind)
        {
            if (Kind != kind)
            {
                throw new InvalidOperationException($"Personal entry {Id} is {Kind}; it cannot take the fields of {kind}.");
            }
        }

        private void ApplyFields(PersonalEntryFields fields)
        {
            CategoryId = fields.CategoryId;
            ApplyAmountDateAndNote(fields);
        }

        private void ApplyAmountDateAndNote(EntryAmountDateAndNote fields)
        {
            AmountMinor = fields.Amount.MinorUnits;
            Currency = fields.Amount.Currency;
            EntryDate = fields.EntryDate;
            Note = fields.Note;
        }
    }
}
