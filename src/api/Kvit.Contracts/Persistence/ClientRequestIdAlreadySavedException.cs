namespace Kvit.Contracts.Persistence
{
    public sealed class ClientRequestIdAlreadySavedException(Exception uniqueViolation)
        : Exception("Another request saved an expense with the same clientRequestId between this request's duplicate check and its save; nothing of this request was saved.", uniqueViolation);
}
