using Kvit.Domain.Entities;

namespace Kvit.Domain.Cleanup
{
    public static class CleanupRules
    {
        public const int KeptAfterRestoreWindowDays = 30;
        public const int UsageEventLinkMonths = 12;

        public static DateTimeOffset LatestRemovableRowDeletion(DateTimeOffset now)
        {
            return now.AddDays(-(Expense.DeletedExpenseRestoreDays + KeptAfterRestoreWindowDays));
        }

        public static DateTimeOffset LatestRemovableGroupDeletion(DateTimeOffset now)
        {
            return now.AddDays(-(Group.DeletedGroupRestoreDays + KeptAfterRestoreWindowDays));
        }

        public static DateTimeOffset OldestLinkedUsageEvent(DateTimeOffset now)
        {
            return now.AddMonths(-UsageEventLinkMonths);
        }
    }
}
