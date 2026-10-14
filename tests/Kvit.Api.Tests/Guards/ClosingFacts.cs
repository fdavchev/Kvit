using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;

namespace Kvit.Api.Tests.Guards
{
    public sealed record ClosingFacts(string? Status, string? StartedAtUtc, string? FinishedAtUtc, int ResponseCount, int CancelledEventCount, int FinishedEventCount)
    {
        public static async Task<ClosingFacts> ReadAsync(ExpensesApp app, Guid groupId)
        {
            return new ClosingFacts(
                await ClosingRows.StatusAsync(app, groupId),
                await ClosingRows.ClosingStartedAtUtcAsync(app, groupId),
                await ClosingRows.FinishedAtUtcAsync(app, groupId),
                await ClosingResponseRows.CountAsync(app, groupId),
                await GroupRows.CountEventsOfTypeAsync(app, groupId, "ClosingCancelled"),
                await GroupRows.CountEventsOfTypeAsync(app, groupId, "GroupFinished"));
        }
    }
}
