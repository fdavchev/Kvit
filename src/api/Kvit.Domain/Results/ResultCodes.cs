namespace Kvit.Domain.Results
{
    public static class ResultCodes
    {
        public const string MONEY_CURRENCY_MISMATCH = "MONEY_CURRENCY_MISMATCH";
        public const string MONEY_NOT_ON_CURRENCY_STEP = "MONEY_NOT_ON_CURRENCY_STEP";
        public const string EXPENSE_AMOUNT_NOT_POSITIVE = "EXPENSE_AMOUNT_NOT_POSITIVE";
        public const string EXPENSE_SPLIT_NO_PARTICIPANTS = "EXPENSE_SPLIT_NO_PARTICIPANTS";
        public const string EXPENSE_SPLIT_DUPLICATE_MEMBER = "EXPENSE_SPLIT_DUPLICATE_MEMBER";
        public const string EXPENSE_SPLIT_NEGATIVE_INPUT = "EXPENSE_SPLIT_NEGATIVE_INPUT";
        public const string EXPENSE_SPLIT_EXTRAS_EXCEED_TOTAL = "EXPENSE_SPLIT_EXTRAS_EXCEED_TOTAL";
        public const string EXPENSE_SPLIT_DOES_NOT_ADD_UP = "EXPENSE_SPLIT_DOES_NOT_ADD_UP";
        public const string EXPENSE_SPLIT_NO_SHARES = "EXPENSE_SPLIT_NO_SHARES";
    }
}
