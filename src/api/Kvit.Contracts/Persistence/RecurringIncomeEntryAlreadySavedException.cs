namespace Kvit.Contracts.Persistence
{
    public sealed class RecurringIncomeEntryAlreadySavedException(Exception uniqueViolation)
        : Exception("Another request saved an entry of the same repeating income for the same date between this request's check and its save; nothing of this request was saved.", uniqueViolation);
}
