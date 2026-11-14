using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public static class SuspendedCalls
    {
        public static IReadOnlyList<SuspendedCall> All { get; } =
        [
            new SuspendedCall("ListGroups", false, GroupRequests.ListAsync),
            new SuspendedCall("CreateGroup", false, client => GroupRequests.CreateAsync(client)),
            new SuspendedCall("GetGroup", false, client => GroupRequests.GetAsync(client, Guid.NewGuid())),
            new SuspendedCall("UpdateGroup", false, client => GroupRequests.UpdateAsync(client, Guid.NewGuid())),
            new SuspendedCall("DeleteGroup", false, client => GroupRequests.DeleteAsync(client, Guid.NewGuid())),
            new SuspendedCall("LeaveGroup", false, client => GroupRequests.LeaveAsync(client, Guid.NewGuid())),
            new SuspendedCall("ListMembers", false, client => GroupRequests.ListMembersAsync(client, Guid.NewGuid())),
            new SuspendedCall("AddPlainName", false, client => GroupRequests.AddMemberAsync(client, Guid.NewGuid(), "Petre")),
            new SuspendedCall("ListExpenses", false, client => ExpenseRequests.ListAsync(client, Guid.NewGuid())),
            new SuspendedCall("Balances", false, client => BalanceRequests.GetAsync(client, Guid.NewGuid())),
            new SuspendedCall("ListPayments", false, client => SettlementRequests.ListAsync(client, Guid.NewGuid())),
            new SuspendedCall("CountsInBudget", false, client => BudgetShareRequests.SetAsync(client, Guid.NewGuid(), false)),
            new SuspendedCall("JoinWithInvite", false, client => GroupRequests.JoinAsync(client, "token")),
            new SuspendedCall("Dashboard", false, client => client.GetAsync("/api/dashboard", TestContext.Current.CancellationToken)),
            new SuspendedCall("PersonalEntries", false, client => client.GetAsync("/api/personal/entries", TestContext.Current.CancellationToken)),
            new SuspendedCall("AcceptTerms", false, client => client.PostAsync("/api/me/terms/accept", null, TestContext.Current.CancellationToken)),
            new SuspendedCall("ChangeLanguage", false, client => AuthRequests.ChangeLanguageAsync(client, "mk")),
            new SuspendedCall("RecordActive", false, client => client.PostAsync("/api/me/active", null, TestContext.Current.CancellationToken)),
            new SuspendedCall("ChangePassword", false, client => AuthRequests.ChangePasswordAsync(client, RegistrationForm.ValidPassword, "Sunce2027")),
            new SuspendedCall("SetPassword", false, client => AuthRequests.SetPasswordAsync(client, "Sunce2027")),
            new SuspendedCall("UpgradeGuest", false, client => AuthRequests.UpgradeGuestAsync(client, RegistrationForm.UniqueEmail(), RegistrationForm.ValidPassword)),
            new SuspendedCall("AdminStats", true, client => client.GetAsync("/api/admin/stats", TestContext.Current.CancellationToken)),
            new SuspendedCall("OwnerToolsPeople", true, client => OwnerToolsRequests.PeopleAsync(client, "ab")),
            new SuspendedCall("OwnerToolsGroup", true, client => OwnerToolsRequests.GroupAsync(client, Guid.NewGuid())),
            new SuspendedCall("OwnerToolsLog", true, client => OwnerToolsRequests.LogAsync(client)),
            new SuspendedCall("OwnerToolsSuspend", true, client => OwnerToolsRequests.SuspendAsync(client, Guid.NewGuid())),
            new SuspendedCall("OwnerToolsClose", true, client => OwnerToolsRequests.CloseAsync(client, Guid.NewGuid())),
        ];

        public static TheoryData<string> Names => NamesOf(All);

        public static TheoryData<string> NamesForAnOrdinaryPerson => NamesOf(All.Where(call => !call.NeedsAdmin));

        public static SuspendedCall Named(string name)
        {
            return All.SingleOrDefault(call => call.Name == name)
                ?? throw new ArgumentException($"No suspended-person call is named '{name}'. The names are: {string.Join(", ", All.Select(call => call.Name))}.", nameof(name));
        }

        private static TheoryData<string> NamesOf(IEnumerable<SuspendedCall> calls)
        {
            TheoryData<string> data = [];
            foreach (SuspendedCall call in calls)
            {
                data.Add(call.Name);
            }

            return data;
        }
    }
}
