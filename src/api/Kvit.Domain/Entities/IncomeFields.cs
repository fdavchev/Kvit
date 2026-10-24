using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public sealed class IncomeFields : EntryAmountDateAndNote
    {
        private IncomeFields(EntryAmountDateAndNote amountDateAndNote)
            : base(amountDateAndNote)
        {
        }

        public static Result<IncomeFields> Create(IncomeInput input, DateOnly utcToday)
        {
            return From(EntryAmountDateAndNote.Check(input, utcToday));
        }

        public static Result<IncomeFields> CreateForEdit(IncomeInput input, DateOnly utcToday, DateOnly currentEntryDate)
        {
            return From(EntryAmountDateAndNote.CheckForEdit(input, utcToday, currentEntryDate));
        }

        private static Result<IncomeFields> From(Result<EntryAmountDateAndNote> amountDateAndNote)
        {
            if (!amountDateAndNote.IsSuccess)
            {
                return amountDateAndNote.ToFailure<IncomeFields>();
            }

            return Result.Ok(new IncomeFields(amountDateAndNote.Value));
        }
    }
}
