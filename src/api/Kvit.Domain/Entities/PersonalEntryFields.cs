using Kvit.Domain.MoneyRules;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public sealed class PersonalEntryFields
    {
        private PersonalEntryFields(Guid categoryId, IncomeFields amountDateAndNote)
        {
            CategoryId = categoryId;
            Amount = amountDateAndNote.Amount;
            EntryDate = amountDateAndNote.EntryDate;
            Note = amountDateAndNote.Note;
        }

        public Guid CategoryId { get; }

        public Money Amount { get; }

        public DateOnly EntryDate { get; }

        public string? Note { get; }

        public static Result<PersonalEntryFields> Create(PersonalEntryInput input, DateOnly utcToday)
        {
            return WithCategory(input, IncomeFields.Create(input.AmountDateAndNote(), utcToday));
        }

        public static Result<PersonalEntryFields> CreateForEdit(PersonalEntryInput input, DateOnly utcToday, DateOnly currentEntryDate)
        {
            return WithCategory(input, IncomeFields.CreateForEdit(input.AmountDateAndNote(), utcToday, currentEntryDate));
        }

        private static Result<PersonalEntryFields> WithCategory(PersonalEntryInput input, Result<IncomeFields> amountDateAndNote)
        {
            if (!amountDateAndNote.IsSuccess)
            {
                return amountDateAndNote.ToFailure<PersonalEntryFields>();
            }

            if (input.CategoryId is not Guid categoryId)
            {
                return Result.Failure<PersonalEntryFields>("A spending entry needs a category, but none was sent.", ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            }

            return Result.Ok(new PersonalEntryFields(categoryId, amountDateAndNote.Value));
        }
    }
}
