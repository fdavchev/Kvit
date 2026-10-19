using Kvit.Domain.MoneyRules;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public sealed class PersonalEntryFields
    {
        private PersonalEntryFields(Guid categoryId, Money amount, DateOnly entryDate, string? note)
        {
            CategoryId = categoryId;
            Amount = amount;
            EntryDate = entryDate;
            Note = note;
        }

        public Guid CategoryId { get; }

        public Money Amount { get; }

        public DateOnly EntryDate { get; }

        public string? Note { get; }

        public static Result<PersonalEntryFields> Create(PersonalEntryInput input, DateOnly utcToday)
        {
            Result<Currency> currency = ExpenseFields.CheckCurrency(input.Currency);
            if (!currency.IsSuccess)
            {
                return currency.ToFailure<PersonalEntryFields>();
            }

            Result<Money> amount = ExpenseFields.CheckAmount(input.AmountMinor, currency.Value);
            if (!amount.IsSuccess)
            {
                return amount.ToFailure<PersonalEntryFields>();
            }

            Result<DateOnly> entryDate = ExpenseFields.CheckDate(input.EntryDate, utcToday, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
            if (!entryDate.IsSuccess)
            {
                return entryDate.ToFailure<PersonalEntryFields>();
            }

            Result<string?> note = ExpenseFields.CheckNote(input.Note);
            if (!note.IsSuccess)
            {
                return note.ToFailure<PersonalEntryFields>();
            }

            return Result.Ok(new PersonalEntryFields(input.CategoryId, amount.Value, entryDate.Value, note.Value));
        }
    }
}
