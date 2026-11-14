namespace Kvit.Api.Tests.AccountDeletion
{
    public sealed record PersonalCounts(int Entries, int RecurringIncomes, int Limits, int Categories, int Answers)
    {
        public static readonly PersonalCounts Empty = new(0, 0, 0, 0, 0);

        public static readonly PersonalCounts Full = new(4, 1, 2, 2, 1);
    }
}
