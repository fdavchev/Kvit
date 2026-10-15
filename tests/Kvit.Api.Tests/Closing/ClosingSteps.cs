using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public static class ClosingSteps
    {
        public static readonly TimeSpan ClosingWindow = TimeSpan.FromHours(24);
        public static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
        public const int ExpensesInTheSetup = 1;

        public static async Task<SettlementGroup> CreateGroupWithAnExpenseAsync(ExpensesApp app)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(app);
            await AddCountedExpenseAsync(app, group.GroupId, group.Owner, group.OwnerRowId);

            return group;
        }

        public static async Task<SettlementGroup> CreateClosingGroupAsync(ExpensesApp app)
        {
            SettlementGroup group = await CreateGroupWithAnExpenseAsync(app);
            await StartAsync(group.Owner, group.GroupId);

            return group;
        }

        public static async Task<SettlementGroup> CreateFinishedGroupAsync(ExpensesApp app)
        {
            SettlementGroup group = await CreateClosingGroupAsync(app);
            await ConfirmAsync(group.Member, group.GroupId);
            await ConfirmAsync(group.Third, group.GroupId);

            return group;
        }

        public static async Task<LoneOwnerGroup> CreateGroupWithPlainNamesOnlyAsync(ExpensesApp app, params string[] plainNames)
        {
            SignedInUser owner = await app.SignUpAsync();
            Guid groupId = await app.CreateGroupAsync(owner);
            Guid ownerRowId = await GroupRows.CurrentMemberIdAsync(app, groupId, owner.UserId);
            foreach (string plainName in plainNames)
            {
                await GroupRows.InsertMemberAsync(app, groupId, null, plainName, owner.UserId);
            }

            await AddCountedExpenseAsync(app, groupId, owner, ownerRowId);

            return new LoneOwnerGroup(groupId, owner, ownerRowId);
        }

        public static async Task AddCountedExpenseAsync(ExpensesApp app, Guid groupId, SignedInUser owner, Guid ownerRowId)
        {
            Guid expenseId = await ExpenseRows.InsertExpenseAsync(app, groupId, ownerRowId, owner.UserId);
            await ExpenseRows.InsertShareAsync(app, expenseId, ownerRowId, shareMinor: ExpenseRows.DefaultAmountMinor);
        }

        public static async Task<Guid> CreateEmptyOneBillAsync(ExpensesApp app, SignedInUser owner)
        {
            Guid groupId = await GroupRows.InsertGroupAsync(app, owner.UserId, kind: "OneBill", name: "Bill");
            await GroupRows.InsertMemberAsync(app, groupId, owner.UserId, GroupRows.OwnerMemberName, owner.UserId);

            return groupId;
        }

        public static async Task StartAsync(SignedInUser owner, Guid groupId)
        {
            await SettlementSteps.AssertNoContentAsync(await ClosingRequests.StartAsync(owner.Client, groupId));
        }

        public static async Task ConfirmAsync(SignedInUser member, Guid groupId)
        {
            await SettlementSteps.AssertNoContentAsync(await ClosingRequests.ConfirmAsync(member.Client, groupId));
        }

        public static async Task ObjectAsync(SignedInUser member, Guid groupId, string? reason = null)
        {
            await SettlementSteps.AssertNoContentAsync(await ClosingRequests.ObjectAsync(member.Client, groupId, reason));
        }

        public static async Task UnlockAsync(SignedInUser owner, Guid groupId)
        {
            await SettlementSteps.AssertNoContentAsync(await ClosingRequests.UnlockAsync(owner.Client, groupId));
        }

        public static async Task<JsonElement> ReadGroupAsync(SignedInUser viewer, Guid groupId)
        {
            HttpResponseMessage response = await GroupRequests.GetAsync(viewer.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }

        public static async Task<JsonElement> ReadGroupListAsync(SignedInUser viewer)
        {
            HttpResponseMessage response = await GroupRequests.ListAsync(viewer.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }

        public static async Task<string> StatusInGroupResponseAsync(SignedInUser viewer, Guid groupId)
        {
            JsonElement body = await ReadGroupAsync(viewer, groupId);

            return body.GetProperty("status").GetString()!;
        }
    }
}
