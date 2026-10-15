using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Npgsql;

namespace Kvit.Api.Tests.Guards
{
    public static class GuardStates
    {
        public static Task ApplyAsync(ExpensesApp app, SettlementGroup group, GuardedWrite write, string state)
        {
            return state switch
            {
                GuardedWrites.ClosingState => CloseAsync(app, group, write),
                GuardedWrites.FinishedState => ForceFinishedAsync(app, group.GroupId),
                _ => throw new ArgumentException($"'{state}' is not a state the guard tests set up. Use {GuardedWrites.ClosingState} or {GuardedWrites.FinishedState}.", nameof(state)),
            };
        }

        public static async Task CloseAsync(ExpensesApp app, SettlementGroup group, GuardedWrite write)
        {
            if (write.NeedsPendingPayment)
            {
                await ForceClosingAsync(app, group.GroupId);
                await ClosingResponseRows.InsertAsync(app, group.GroupId, group.MemberRowId, respondedAt: app.Clock.GetUtcNow());
                return;
            }

            await ClosingSteps.AddCountedExpenseAsync(app, group.GroupId, group.Owner, group.OwnerRowId);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            if (!write.HasNoMemberToConfirmBeforeTheWrite)
            {
                await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            }
        }

        public static Task ForceClosingAsync(ExpensesApp app, Guid groupId)
        {
            return app.ExecuteAsync(
                "UPDATE groups SET status = 'Closing', closing_started_at = @now, finished_at = NULL WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("now", app.Clock.GetUtcNow()));
        }

        public static Task ForceFinishedAsync(ExpensesApp app, Guid groupId)
        {
            return app.ExecuteAsync(
                "UPDATE groups SET status = 'Finished', closing_started_at = @now, finished_at = @now WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("now", app.Clock.GetUtcNow()));
        }
    }
}
