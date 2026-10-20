namespace Kvit.Api.Tests.PersonalEntries
{
    public sealed class ChangeAfterNextTransactionOpens
    {
        private Func<Task>? _change;

        public void Arm(Func<Task> change)
        {
            if (Interlocked.CompareExchange(ref _change, change, null) is not null)
            {
                throw new InvalidOperationException("A change is already waiting for the next transaction; a test armed it twice.");
            }
        }

        public Task RunIfArmedAsync()
        {
            Func<Task>? change = Interlocked.Exchange(ref _change, null);

            return change is null ? Task.CompletedTask : change();
        }
    }
}
