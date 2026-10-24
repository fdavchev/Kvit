namespace Kvit.Contracts.Persistence
{
    public sealed class BudgetLimitChangedMeanwhileException(Exception saveFailure)
        : Exception("Another request added, changed or removed the same limit row between this request's read and its save; nothing of this request was saved.", saveFailure);
}
