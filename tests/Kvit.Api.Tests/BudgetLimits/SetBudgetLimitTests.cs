using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.PersonalCategories;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.BudgetLimits
{
    public class SetBudgetLimitTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task SetMonth_NewLimit_Answers204WithNoBodyAndStoresOneRowWithoutACategory()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await BudgetLimitRequests.SetMonthAsync(user.Client, 4_500_000);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(4_500_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null));
            Assert.Equal(1, await BudgetLimitSteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Set_AnyLimit_StoresTheFirstDayOfTheCurrentMonthAsTheMonthItStartsApplying()
        {
            SignedInUser user = await _app.SignUpAsync();

            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);

            Assert.Equal(["2026-10-01:4500000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            Assert.Equal(["2026-10-01:800000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task SetMonth_SecondLimit_ReplacesTheFirstAndKeepsOneRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);

            HttpResponseMessage response = await BudgetLimitRequests.SetMonthAsync(user.Client, 3_000_000);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(3_000_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null));
            Assert.Equal(1, await BudgetLimitSteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task SetMonth_TheSameValueTwice_Answers204BothTimesAndKeepsOneRowWithTheValue()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage first = await BudgetLimitRequests.SetMonthAsync(user.Client, 4_500_000);
            HttpResponseMessage second = await BudgetLimitRequests.SetMonthAsync(user.Client, 4_500_000);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
            Assert.Equal(4_500_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null));
            Assert.Equal(1, await BudgetLimitSteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task SetCategory_BuiltInCategory_Answers204WithNoBodyAndStoresOneRowForThatCategory()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await BudgetLimitRequests.SetCategoryAsync(user.Client, BudgetLimitSteps.Food, 800_000);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(800_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Food));
            Assert.Null(await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null));
            Assert.Equal(1, await BudgetLimitSteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task SetCategory_SecondLimit_ReplacesTheFirstAndKeepsOneRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);

            HttpResponseMessage response = await BudgetLimitRequests.SetCategoryAsync(user.Client, BudgetLimitSteps.Food, 500_000);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(500_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Food));
            Assert.Equal(1, await BudgetLimitSteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task SetCategory_TheSameValueTwice_Answers204BothTimesAndKeepsOneRowWithTheValue()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage first = await BudgetLimitRequests.SetCategoryAsync(user.Client, BudgetLimitSteps.Food, 800_000);
            HttpResponseMessage second = await BudgetLimitRequests.SetCategoryAsync(user.Client, BudgetLimitSteps.Food, 800_000);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
            Assert.Equal(800_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Food));
            Assert.Equal(1, await BudgetLimitSteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Set_MonthAndSeveralCategories_AreSeparateRowsThatDoNotTouchEachOther()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Transport, 200_000);

            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 900_000);

            Assert.Equal(3, await BudgetLimitSteps.CountOfUserAsync(_app, user.UserId));
            Assert.Equal(4_500_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null));
            Assert.Equal(900_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Food));
            Assert.Equal(200_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Transport));
        }

        [Fact]
        public async Task SetCategory_OwnCustomCategory_Answers204AndStoresIt()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");

            HttpResponseMessage response = await BudgetLimitRequests.SetCategoryAsync(user.Client, pets, 300_000);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(300_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, pets));
        }

        [Fact]
        public async Task SetCategory_OwnArchivedCategory_Answers204AndStoresIt()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Pets");

            HttpResponseMessage response = await BudgetLimitRequests.SetCategoryAsync(user.Client, pets, 300_000);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(300_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, pets));
        }

        [Fact]
        public async Task SetCategory_CustomCategoryOfAnotherPerson_Answers404CategoryNotFoundAndStoresNothing()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid ownersPets = await PersonalCategorySteps.CreateAsync(owner.Client, "Pets");

            HttpResponseMessage response = await BudgetLimitRequests.SetCategoryAsync(other.Client, ownersPets, 300_000);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
            Assert.Equal(0, await BudgetLimitSteps.CountOfUserAsync(_app, other.UserId));
            Assert.Equal(0, await BudgetLimitSteps.CountOfUserAsync(_app, owner.UserId));
        }

        [Fact]
        public async Task SetCategory_UnknownCategory_Answers404CategoryNotFoundAndStoresNothing()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await BudgetLimitRequests.SetCategoryAsync(user.Client, Guid.CreateVersion7(), 300_000);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
            Assert.Equal(0, await BudgetLimitSteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Set_FirstTimeAndThenAChange_SavesTheCreationMomentOnceAndTheChangeMoment()
        {
            SignedInUser user = await _app.SignUpAsync();
            string created = BudgetLimitSteps.TextOf(_app.Clock.GetUtcNow());
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            string changed = BudgetLimitSteps.TextOf(_app.Clock.GetUtcNow());

            await BudgetLimitSteps.SetMonthAsync(user, 3_000_000);

            Assert.Equal(created, await BudgetLimitSteps.TimestampAsync(_app, user.UserId, null, "created_at"));
            Assert.Equal(changed, await BudgetLimitSteps.TimestampAsync(_app, user.UserId, null, "updated_at"));
        }

        [Theory]
        [InlineData(BudgetLimitRequests.SmallestLimit)]
        [InlineData(BudgetLimitRequests.LargestWholeDenarLimit)]
        public async Task Set_SmallestAndLargestWholeDenarAmounts_AreAccepted(long limitMinor)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage month = await BudgetLimitRequests.SetMonthAsync(user.Client, limitMinor);
            HttpResponseMessage category = await BudgetLimitRequests.SetCategoryAsync(user.Client, BudgetLimitSteps.Food, limitMinor);

            Assert.Equal(HttpStatusCode.NoContent, month.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, category.StatusCode);
            Assert.Equal(limitMinor, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null));
            Assert.Equal(limitMinor, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        [InlineData(-1)]
        [InlineData(long.MinValue)]
        [InlineData(1)]
        [InlineData(99)]
        [InlineData(101)]
        [InlineData(150_050)]
        [InlineData(999_999_999_999)]
        [InlineData(1_000_000_000_000)]
        [InlineData(long.MaxValue)]
        public async Task Set_AmountThatIsNotAPositiveWholeDenarWithinTheBound_Answers400WithTheBudgetLimitCodeAndKeepsTheStoredLimits(long limitMinor)
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);

            HttpResponseMessage month = await BudgetLimitRequests.SetMonthAsync(user.Client, limitMinor);
            HttpResponseMessage category = await BudgetLimitRequests.SetCategoryAsync(user.Client, BudgetLimitSteps.Food, limitMinor);

            await ProblemResponse.AssertAsync(month, HttpStatusCode.BadRequest, ResultCodes.BUDGET_LIMIT_AMOUNT_INVALID);
            await ProblemResponse.AssertAsync(category, HttpStatusCode.BadRequest, ResultCodes.BUDGET_LIMIT_AMOUNT_INVALID);
            Assert.Equal(4_500_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null));
            Assert.Equal(800_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"limit\":100000}")]
        [InlineData("{\"limitMinor\":null}")]
        [InlineData("{\"limitMinor\":\"abc\"}")]
        [InlineData("{\"limitMinor\":true}")]
        [InlineData("{\"limitMinor\":[]}")]
        [InlineData("{\"limitMinor\":150.5}")]
        [InlineData("{\"limitMinor\":99999999999999999999}")]
        public async Task Set_BodyWithoutAUsableNumber_Answers400WithTheBudgetLimitCodeAndKeepsTheStoredLimits(string json)
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);

            HttpResponseMessage month = await BudgetLimitRequests.SetMonthWithJsonTextAsync(user.Client, json);
            HttpResponseMessage category = await BudgetLimitRequests.SetCategoryWithJsonTextAsync(user.Client, BudgetLimitSteps.Food, json);

            await ProblemResponse.AssertAsync(month, HttpStatusCode.BadRequest, ResultCodes.BUDGET_LIMIT_AMOUNT_INVALID);
            await ProblemResponse.AssertAsync(category, HttpStatusCode.BadRequest, ResultCodes.BUDGET_LIMIT_AMOUNT_INVALID);
            Assert.Equal(4_500_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null));
            Assert.Equal(800_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Theory]
        [InlineData("not json")]
        [InlineData("")]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("\"100000\"")]
        public async Task Set_BrokenOrEmptyBody_Answers400AndKeepsTheStoredLimits(string json)
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);

            HttpResponseMessage month = await BudgetLimitRequests.SetMonthWithJsonTextAsync(user.Client, json);
            HttpResponseMessage category = await BudgetLimitRequests.SetCategoryWithJsonTextAsync(user.Client, BudgetLimitSteps.Food, json);

            Assert.Equal(HttpStatusCode.BadRequest, month.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, category.StatusCode);
            Assert.Equal(4_500_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null));
            Assert.Equal(800_000, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task SetCategory_UnknownCategoryWithABadAmount_AnswersCategoryNotFoundFirst()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await BudgetLimitRequests.SetCategoryAsync(user.Client, Guid.CreateVersion7(), 0);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
        }

        [Fact]
        public async Task Set_ByOnePerson_NeverChangesTheLimitsOfAnotherPerson()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync("Bojan");
            await BudgetLimitSteps.SetMonthAsync(second, 7_000_000);
            await BudgetLimitSteps.SetCategoryAsync(second, BudgetLimitSteps.Food, 600_000);

            await BudgetLimitSteps.SetMonthAsync(first, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(first, BudgetLimitSteps.Food, 800_000);

            Assert.Equal(7_000_000, await BudgetLimitSteps.StoredLimitAsync(_app, second.UserId, null));
            Assert.Equal(600_000, await BudgetLimitSteps.StoredLimitAsync(_app, second.UserId, BudgetLimitSteps.Food));
            Assert.Equal(4_500_000, await BudgetLimitSteps.StoredLimitAsync(_app, first.UserId, null));
            Assert.Equal(800_000, await BudgetLimitSteps.StoredLimitAsync(_app, first.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task SetAndDelete_AnyLimit_WriteNoActivityOrUsageEvent()
        {
            SignedInUser user = await _app.SignUpAsync();
            int before = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);

            await BudgetLimitSteps.SetMonthAsync(user, 4_500_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 800_000);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, 900_000);
            await BudgetLimitRequests.DeleteCategoryAsync(user.Client, BudgetLimitSteps.Food);
            await BudgetLimitRequests.DeleteMonthAsync(user.Client);

            Assert.Equal(before, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
        }
    }
}
