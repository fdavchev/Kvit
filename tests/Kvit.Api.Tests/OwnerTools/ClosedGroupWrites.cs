using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Guards;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public static class ClosedGroupWrites
    {
        public const string DeleteGroup = "DeleteGroup";
        public const string RestoreGroup = "RestoreGroup";
        public const string Leave = "Leave";

        public static TheoryData<string> NamesCalledByMembers => NamesOf(GuardedWrites.All.Where(write => write.Who != WhoMayCall.OwnerOnly && !write.IsAllowedWhenFinished));

        public static TheoryData<string> NamesCalledByTheOwner => NamesOf(
            GuardedWrites.All.Where(write => write.Who != WhoMayCall.MemberOnly && (!write.IsAllowedWhenFinished || write.Name == DeleteGroup) && write.Name != RestoreGroup));

        public static TheoryData<string> ReadNames { get; } =
        [
            "Group",
            "Members",
            "Expenses",
            "DeletedExpenses",
            "Balances",
            "Payments",
            "Activity",
            "Groups",
            "Dashboard",
        ];

        public static string ReadPathOf(string name, Guid groupId)
        {
            return name switch
            {
                "Group" => GroupRequests.PathOf(groupId),
                "Members" => GroupRequests.MembersPathOf(groupId),
                "Expenses" => ExpenseRequests.ExpensesPathOf(groupId),
                "DeletedExpenses" => ExpenseRequests.DeletedPathOf(groupId),
                "Balances" => BalanceRequests.PathOf(groupId),
                "Payments" => SettlementRequests.SettlementsPathOf(groupId),
                "Activity" => ExpenseRequests.ActivityPathOf(groupId),
                "Groups" => GroupRequests.GroupsPath,
                "Dashboard" => "/api/dashboard",
                _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Not one of the reads of a group."),
            };
        }

        private static TheoryData<string> NamesOf(IEnumerable<GuardedWrite> writes)
        {
            TheoryData<string> data = [];
            foreach (GuardedWrite write in writes)
            {
                data.Add(write.Name);
            }

            return data;
        }
    }
}
