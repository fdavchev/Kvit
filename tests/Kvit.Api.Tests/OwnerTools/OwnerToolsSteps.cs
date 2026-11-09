using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;

namespace Kvit.Api.Tests.OwnerTools
{
    public static class OwnerToolsSteps
    {
        public static string UniqueToken()
        {
            return Guid.NewGuid().ToString("N")[..12];
        }

        public static async Task SuspendAsync(SignedInUser admin, Guid personId, string reason = OwnerToolsRequests.DefaultReason)
        {
            await SettlementSteps.AssertNoContentAsync(await OwnerToolsRequests.SuspendAsync(admin.Client, personId, reason));
        }

        public static async Task UnsuspendAsync(SignedInUser admin, Guid personId, string reason = OwnerToolsRequests.DefaultReason)
        {
            await SettlementSteps.AssertNoContentAsync(await OwnerToolsRequests.UnsuspendAsync(admin.Client, personId, reason));
        }

        public static async Task EraseNameAsync(SignedInUser admin, Guid personId, string reason = OwnerToolsRequests.DefaultReason)
        {
            await SettlementSteps.AssertNoContentAsync(await OwnerToolsRequests.EraseNameAsync(admin.Client, personId, reason));
        }

        public static async Task CloseGroupAsync(SignedInUser admin, Guid groupId, string reason = OwnerToolsRequests.DefaultReason)
        {
            await SettlementSteps.AssertNoContentAsync(await OwnerToolsRequests.CloseAsync(admin.Client, groupId, reason));
        }

        public static async Task ReopenGroupAsync(SignedInUser admin, Guid groupId, string reason = OwnerToolsRequests.DefaultReason)
        {
            await SettlementSteps.AssertNoContentAsync(await OwnerToolsRequests.ReopenAsync(admin.Client, groupId, reason));
        }

        public static async Task PrepareAsync(OwnerToolsApp app, OwnerToolsCall call, Guid personId, Guid groupId)
        {
            if (call.Name == OwnerToolsCalls.Unsuspend)
            {
                await OwnerToolsRows.SetSuspendedAtAsync(app, personId, app.Clock.GetUtcNow());
            }

            if (call.Name == OwnerToolsCalls.Reopen)
            {
                await OwnerToolsRows.SetAdminClosedAtAsync(app, groupId, app.Clock.GetUtcNow());
            }
        }

        public static async Task<string> SnapshotAsync(OwnerToolsApp app, Guid personId, Guid groupId)
        {
            string person = await DeletionRows.SnapshotOfUserRowAsync(app, personId);
            string group = await GroupRows.SnapshotAsync(app, groupId);

            return $"{person}@{group}";
        }
    }
}
