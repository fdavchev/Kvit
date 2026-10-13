using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;

namespace Kvit.Api.Tests.Groups
{
    public static class ZeroBalanceScenario
    {
        public const long ExpenseMinor = 60_000;
        public const long ShareMinor = 30_000;
        public const string PendingStatus = "Pending";
        public const string ConfirmedStatus = "Confirmed";
        public const string RejectedStatus = "Rejected";
        public const string CancelledStatus = "Cancelled";

        public static Task<Guid> AddTwoPersonExpenseAsync(ExpensesApp app, SettlementGroup group, Guid payerRowId, Guid otherRowId, string currency = ExpenseInputs.Mkd)
        {
            return app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseMinor, currency, payerRowId, payerRowId, otherRowId));
        }

        public static Task<Guid> BojanOwesAnaAsync(ExpensesApp app, SettlementGroup group, string currency = ExpenseInputs.Mkd)
        {
            return AddTwoPersonExpenseAsync(app, group, group.OwnerRowId, group.MemberRowId, currency);
        }

        public static Task<Guid> AnaOwesBojanAsync(ExpensesApp app, SettlementGroup group, string currency = ExpenseInputs.Mkd)
        {
            return AddTwoPersonExpenseAsync(app, group, group.MemberRowId, group.OwnerRowId, currency);
        }

        public static Task<Guid> MarkoOwesAnaAsync(ExpensesApp app, SettlementGroup group, string currency = ExpenseInputs.Mkd)
        {
            return AddTwoPersonExpenseAsync(app, group, group.OwnerRowId, group.MarkoRowId, currency);
        }

        public static Task<Guid> BojanPaysAnaInFullAsync(SettlementGroup group, string currency = ExpenseInputs.Mkd)
        {
            return SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, ShareMinor, currency);
        }

        public static Task<Guid> BojanPaysAnaWithStatusAsync(SettlementGroup group, string status)
        {
            return RecordWithStatusAsync(group, status, group.Member, group.MemberRowId, group.Owner, group.OwnerRowId);
        }

        public static Task<Guid> AnaPaysBojanWithStatusAsync(SettlementGroup group, string status)
        {
            return RecordWithStatusAsync(group, status, group.Owner, group.OwnerRowId, group.Member, group.MemberRowId);
        }

        public static async Task<string> SnapshotAsync(ExpensesApp app, Guid groupId)
        {
            string groupPart = await GroupRows.SnapshotAsync(app, groupId);
            string settlementPart = await SettlementRows.SnapshotAsync(app, groupId);
            string expensePart = await ExpenseRows.SnapshotAsync(app, groupId);

            return $"{groupPart}@{settlementPart}@{expensePart}";
        }

        private static async Task<Guid> RecordWithStatusAsync(SettlementGroup group, string status, SignedInUser payer, Guid payerRowId, SignedInUser receiver, Guid receiverRowId)
        {
            if (status == ConfirmedStatus)
            {
                return await SettlementSteps.RecordAsync(receiver, group.GroupId, payerRowId, receiverRowId);
            }

            Guid settlementId = await SettlementSteps.RecordAsync(payer, group.GroupId, payerRowId, receiverRowId);
            if (status == PendingStatus)
            {
                return settlementId;
            }

            if (status == RejectedStatus)
            {
                await SettlementSteps.RejectAsync(receiver, group.GroupId, settlementId);
                return settlementId;
            }

            if (status == CancelledStatus)
            {
                await SettlementSteps.CancelAsync(payer, group.GroupId, settlementId);
                return settlementId;
            }

            throw new ArgumentException($"{status} is not a payment status.", nameof(status));
        }
    }
}
