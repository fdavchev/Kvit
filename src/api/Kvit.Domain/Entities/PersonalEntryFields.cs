using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public sealed class PersonalEntryFields : EntryAmountDateAndNote
    {
        private PersonalEntryFields(Guid categoryId, EntryAmountDateAndNote amountDateAndNote)
            : base(amountDateAndNote)
        {
            CategoryId = categoryId;
        }

        public Guid CategoryId { get; }

        public static Result<PersonalEntryFields> Create(PersonalEntryInput input, DateOnly utcToday)
        {
            return WithCategory(input, EntryAmountDateAndNote.Check(input.AmountDateAndNote(), utcToday));
        }

        public static Result<PersonalEntryFields> CreateForEdit(PersonalEntryInput input, DateOnly utcToday, DateOnly currentEntryDate)
        {
            return WithCategory(input, EntryAmountDateAndNote.CheckForEdit(input.AmountDateAndNote(), utcToday, currentEntryDate));
        }

        private static Result<PersonalEntryFields> WithCategory(PersonalEntryInput input, Result<EntryAmountDateAndNote> amountDateAndNote)
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
