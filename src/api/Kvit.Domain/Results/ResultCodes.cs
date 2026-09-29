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
        public const string AUTH_INVALID_CREDENTIALS = "AUTH_INVALID_CREDENTIALS";
        public const string AUTH_EMAIL_TAKEN = "AUTH_EMAIL_TAKEN";
        public const string AUTH_EMAIL_INVALID = "AUTH_EMAIL_INVALID";
        public const string AUTH_PASSWORD_TOO_WEAK = "AUTH_PASSWORD_TOO_WEAK";
        public const string AUTH_DISPLAY_NAME_INVALID = "AUTH_DISPLAY_NAME_INVALID";
        public const string AUTH_LOCKED_OUT = "AUTH_LOCKED_OUT";
        public const string AUTH_NOT_SIGNED_IN = "AUTH_NOT_SIGNED_IN";
        public const string TIME_ZONE_INVALID = "TIME_ZONE_INVALID";
        public const string LANGUAGE_INVALID = "LANGUAGE_INVALID";
    }
}
