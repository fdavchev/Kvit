namespace Kvit.Contracts.Persistence
{
    public sealed class AccountChangedMeanwhileException : Exception
    {
        private const string Explanation = "Another request changed the same account between this request's read and its save; nothing of this request was saved.";

        public AccountChangedMeanwhileException(string identityFailure)
            : base($"{Explanation} Identity answered: {identityFailure}")
        {
        }

        public AccountChangedMeanwhileException(Exception concurrencyFailure)
            : base(Explanation, concurrencyFailure)
        {
        }
    }
}
