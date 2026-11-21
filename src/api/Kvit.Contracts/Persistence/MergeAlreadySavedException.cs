namespace Kvit.Contracts.Persistence
{
    public sealed class MergeAlreadySavedException(Exception uniqueViolation)
        : Exception("Another request merged the same personal entry or the same expense of this person with something else between this request's merge check and its save; nothing of this request was saved.", uniqueViolation);
}
