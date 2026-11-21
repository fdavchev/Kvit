namespace Kvit.Contracts.Persistence
{
    public sealed class SettlementChangedMeanwhileException(Exception concurrencyFailure)
        : Exception("Another request answered, deleted or restored the same payment between this request's read and its save; nothing of this request was saved.", concurrencyFailure);
}
