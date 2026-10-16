namespace Kvit.Contracts.Persistence
{
    public sealed class MemberChangedMeanwhileException(Exception concurrencyFailure)
        : Exception("Another request changed the account of a group member between this request's read and its save; nothing of this request was saved.", concurrencyFailure);
}
