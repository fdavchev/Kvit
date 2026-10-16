namespace Kvit.Contracts.Persistence
{
    public sealed class ActiveDayAlreadySavedException(Exception uniqueViolation)
        : Exception("Another request saved this user's Active usage event for the same UTC day between this request's check and its save; nothing of this request was saved.", uniqueViolation);
}
