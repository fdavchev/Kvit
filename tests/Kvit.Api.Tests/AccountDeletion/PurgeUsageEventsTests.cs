using Kvit.Api.Tests.Dashboard;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeUsageEventsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Purge_UsageEventsOfThePerson_KeepTheirRowsWithTheUserIdSetToNull()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DashboardRequests.ActiveAsync(user.Client);
            List<string?> idsBefore = await UsageEventIdsAsync(user.UserId);
            Assert.NotEmpty(idsBefore);

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Empty(await UsageEventIdsAsync(user.UserId));
            foreach (string? id in idsBefore)
            {
                List<string?> userIds = await _app.QueryAsync(
                    "SELECT coalesce(user_id::text, 'null') FROM usage_events WHERE id = @id",
                    new NpgsqlParameter("id", long.Parse(id!)));
                Assert.Equal("null", Assert.Single(userIds));
            }
        }

        [Fact]
        public async Task Purge_UsageEvents_TheTotalCountStaysTheSame()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DashboardRequests.ActiveAsync(user.Client);
            int totalBefore = await _app.CountUsageEventsAsync();

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.FormerMember);

            Assert.Equal(totalBefore, await _app.CountUsageEventsAsync());
        }

        [Fact]
        public async Task Purge_UsageEvents_KeepTheirTypeDetailAndDay()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DashboardRequests.ActiveAsync(user.Client);
            long[] ids = [.. (await UsageEventIdsAsync(user.UserId)).Select(id => long.Parse(id!))];
            string before = await ContentOfAsync(ids);

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.NotEqual(string.Empty, before);
            Assert.Equal(before, await ContentOfAsync(ids));
        }

        [Fact]
        public async Task Purge_UsageEventsOfOtherPeople_AreUntouched()
        {
            SignedInUser leaver = await _app.SignUpAsync("Zorica");
            SignedInUser other = await _app.SignUpAsync("Bojan");
            await DashboardRequests.ActiveAsync(other.Client);
            List<string?> otherIdsBefore = await UsageEventIdsAsync(other.UserId);
            long[] ids = [.. otherIdsBefore.Select(id => long.Parse(id!))];
            string otherBefore = await ContentOfAsync(ids);

            await DeletionSteps.RequestAndPurgeAsync(_app, leaver.Client, leaver.UserId, DeletionRequests.KeepName);

            Assert.Equal(otherIdsBefore, await UsageEventIdsAsync(other.UserId));
            Assert.Equal(otherBefore, await ContentOfAsync(ids));
        }

        [Fact]
        public async Task Purge_PersonWithoutAnyUsageEvent_Succeeds()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.ExecuteAsync("DELETE FROM usage_events WHERE user_id = @id", new NpgsqlParameter("id", user.UserId));

            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal(now, (await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        private async Task<List<string?>> UsageEventIdsAsync(Guid userId)
        {
            return await _app.QueryAsync("SELECT id::text FROM usage_events WHERE user_id = @id ORDER BY id", new NpgsqlParameter("id", userId));
        }

        private async Task<string> ContentOfAsync(long[] ids)
        {
            List<string?> rows = await _app.QueryAsync(
                "SELECT coalesce(string_agg(type || ':' || coalesce(detail, '') || ':' || occurred_on::text, '|' ORDER BY id), '') FROM usage_events WHERE id = ANY(@ids)",
                new NpgsqlParameter("ids", ids));

            return Assert.Single(rows)!;
        }
    }
}
