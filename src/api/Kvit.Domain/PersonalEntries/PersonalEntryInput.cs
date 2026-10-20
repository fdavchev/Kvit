using Kvit.Domain.Results;

namespace Kvit.Domain.PersonalEntries
{
    public sealed record PersonalEntryInput(
        Guid? CategoryId,
        long AmountMinor,
        string Currency,
        string EntryDate,
        string? Note)
    {
        public IncomeInput AmountDateAndNote()
        {
            return new IncomeInput(AmountMinor, Currency, EntryDate, Note);
        }

        public Result<IncomeInput> ToIncomeInput()
        {
            if (CategoryId is Guid categoryId)
            {
                return Result.Failure<IncomeInput>($"Income has no category, but category {categoryId} was sent with it.", ResultCodes.PERSONAL_ENTRY_INCOME_HAS_CATEGORY);
            }

            return Result.Ok(AmountDateAndNote());
        }
    }
}
