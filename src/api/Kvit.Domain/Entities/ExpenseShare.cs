using Kvit.Domain.MoneyRules;

namespace Kvit.Domain.Entities
{
    public class ExpenseShare
    {
        private ExpenseShare()
        {
        }

        public Guid ExpenseId { get; private set; }

        public Guid MemberId { get; private set; }

        public long InputValue { get; private set; }

        public long ShareMinor { get; private set; }

        public static ExpenseShare Create(Guid expenseId, SplitShare share)
        {
            return new ExpenseShare
            {
                ExpenseId = expenseId,
                MemberId = share.MemberId,
                InputValue = share.InputValue,
                ShareMinor = share.Share.MinorUnits,
            };
        }

        public void Change(SplitShare share)
        {
            InputValue = share.InputValue;
            ShareMinor = share.Share.MinorUnits;
        }
    }
}
