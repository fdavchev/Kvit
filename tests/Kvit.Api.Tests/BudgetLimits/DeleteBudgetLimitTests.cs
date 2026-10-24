using System.Net;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.PersonalCategories;
using Xunit;

namespace Kvit.Api.Tests.BudgetLimits
{
    public class DeleteBudgetLimitTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task DeleteMonth_ExistingLimit_Answers204WithNoBodyAndRemovesOnlyTheMonthRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);

            HttpResponseMessage response = await BudgetLimitRequests.DeleteMonthAsync(user.Client);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Null(await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null));
            Assert.Equal(800_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task DeleteCategory_ExistingLimit_Answers204WithNoBodyAndRemovesOnlyThatCategoryRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Transport, 200_000);

            HttpResponseMessage response = await BudgetLimitRequests.DeleteCategoryAsync(user.Client, BudgetLimitSteps.Food);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Null(await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Food));
            Assert.Equal(4_500_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null));
            Assert.Equal(200_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Transport));
        }

        [Fact]
        public async Task DeleteMonth_NoLimitSet_Answers204AndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);
            string before = await BudgetLimitSteps.SnapshotAsync(_app);

            HttpResponseMessage response = await BudgetLimitRequests.DeleteMonthAsync(user.Client);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await BudgetLimitSteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task DeleteCategory_NoLimitSet_Answers204AndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            string before = await BudgetLimitSteps.SnapshotAsync(_app);

            HttpResponseMessage response = await BudgetLimitRequests.DeleteCategoryAsync(user.Client, BudgetLimitSteps.Food);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await BudgetLimitSteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Delete_TheSameLimitTwice_Answers204BothTimes()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);

            HttpResponseMessage firstMonth = await BudgetLimitRequests.DeleteMonthAsync(user.Client);
            HttpResponseMessage secondMonth = await BudgetLimitRequests.DeleteMonthAsync(user.Client);
            HttpResponseMessage firstCategory = await BudgetLimitRequests.DeleteCategoryAsync(user.Client, BudgetLimitSteps.Food);
            HttpResponseMessage secondCategory = await BudgetLimitRequests.DeleteCategoryAsync(user.Client, BudgetLimitSteps.Food);

            Assert.Equal(HttpStatusCode.NoContent, firstMonth.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, secondMonth.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, firstCategory.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, secondCategory.StatusCode);
            Assert.Equal(0, await BudgetLimitSteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task DeleteCategory_UnknownCategory_Answers204AndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            string before = await BudgetLimitSteps.SnapshotAsync(_app);

            HttpResponseMessage response = await BudgetLimitRequests.DeleteCategoryAsync(user.Client, Guid.CreateVersion7());

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await BudgetLimitSteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task DeleteCategory_CustomCategoryOfAnotherPerson_Answers204AndChangesNothing()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid ownersPets = await PersonalCategorySteps.CreateAsync(owner.Client, "Pets");
            await BudgetLimitSteps.SetCategoryAsync(owner, ownersPets, 300_000);
            string before = await BudgetLimitSteps.SnapshotAsync(_app);

            HttpResponseMessage response = await BudgetLimitRequests.DeleteCategoryAsync(other.Client, ownersPets);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await BudgetLimitSteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task DeleteCategory_OwnArchivedCategory_RemovesItsLimit()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            await BudgetLimitSteps.SetCategoryAsync(user, pets, 300_000);
            await PersonalCategoryRequests.ArchiveAsync(user.Client, pets);

            HttpResponseMessage response = await BudgetLimitRequests.DeleteCategoryAsync(user.Client, pets);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, pets));
        }

        [Fact]
        public async Task Delete_ByOnePerson_NeverRemovesTheSameLimitOfAnotherPerson()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync("Bojan");
            await BudgetLimitSteps.SetMonthAsync(first, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(first, BudgetLimitSteps.Food, 800_000);
            await BudgetLimitSteps.SetMonthAsync(second, 7_000_000);
            await BudgetLimitSteps.SetCategoryAsync(second, BudgetLimitSteps.Food, 600_000);

            await BudgetLimitRequests.DeleteMonthAsync(first.Client);
            await BudgetLimitRequests.DeleteCategoryAsync(first.Client, BudgetLimitSteps.Food);

            Assert.Equal(0, await BudgetLimitSteps.CountOfUserAsync(_app, first.UserId));
            Assert.Equal(7_000_000, await BudgetLimitSteps.StoredLimitAsync(_app, second.UserId, null));
            Assert.Equal(600_000, await BudgetLimitSteps.StoredLimitAsync(_app, second.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task Set_AfterDelete_CreatesAFreshRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            await BudgetLimitRequests.DeleteMonthAsync(user.Client);

            await BudgetLimitSteps.SetMonthAsync(user, 3_000_000);

            Assert.Equal(3_000_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null));
            Assert.Equal(1, await BudgetLimitSteps.CountOfUserAsync(_app, user.UserId));
        }
    }
}
