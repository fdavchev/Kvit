namespace Kvit.Contracts.Persistence
{
    public sealed class ExpenseChangedMeanwhileException(Exception saveFailure)
        : Exception("Another request changed, deleted or restored the same expense between this request's read and its save; nothing of this request was saved.", saveFailure);
}
