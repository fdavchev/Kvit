namespace Kvit.Contracts.Persistence
{
    public sealed class CategoryNameAlreadySavedException(Exception uniqueViolation)
        : Exception("Another request gave one of this person's active categories the same name, ignoring letter case, between this request's name check and its save; nothing of this request was saved.", uniqueViolation);
}
