namespace Kvit.Contracts.Persistence
{
    public sealed class DeadlockDetectedException(Exception deadlock)
        : Exception("Postgres stopped this request's save because it and another transaction were each waiting for the other (a deadlock); nothing of this request was saved.", deadlock);
}
