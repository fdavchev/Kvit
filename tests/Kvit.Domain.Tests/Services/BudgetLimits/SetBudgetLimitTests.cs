using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.BudgetLimits;
using Kvit.Domain.Tests.Entities;
using Kvit.Domain.Tests.Fakes;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Services.BudgetLimits
{
    public class SetBudgetLimitTests
    {
        private const string Skopje = "Europe/Skopje";
        private static readonly DateTimeOffset LastHourOfOctoberInSkopje = new(2026, 10, 31, 22, 30, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset FirstHourOfNovemberInSkopje = new(2026, 10, 31, 23, 30, 0, TimeSpan.Zero);

        private readonly FakeBudgetLimitRepository _limits = new();
        private readonly FakeCategoryRepository _categories = new();
        private readonly Category _pets = CategoryTestData.NewCustom("Pets");

        public SetBudgetLimitTests()
        {
            _categories.Seed(_pets);
        }

        [Fact]
        public async Task Execute_NoRowInTheCurrentMonth_AddsOneFromTheFirstDayOfTheMonthInTheUsersTimeZone()
        {
            Result result = await ExecuteAsync(FirstHourOfNovemberInSkopje, null, 800_000);

            Assert.True(result.IsSuccess);
            BudgetLimit added = Assert.Single(_limits.Added);
            Assert.Equal(CategoryTestData.OwnerId, added.UserId);
            Assert.Null(added.CategoryId);
            Assert.Equal(new DateOnly(2026, 11, 1), added.EffectiveFrom);
            Assert.Equal(800_000, added.LimitMinor);
            Assert.Equal(FirstHourOfNovemberInSkopje, added.CreatedAt);
        }

        [Fact]
        public async Task Execute_RowInTheCurrentMonth_ChangesThatRowAndAddsNothing()
        {
            BudgetLimit october = BudgetLimit.Create(CategoryTestData.OwnerId, _pets.Id, new DateOnly(2026, 10, 1), 800_000, LastHourOfOctoberInSkopje.AddDays(-5));
            _limits.Seed(october);

            Result result = await ExecuteAsync(LastHourOfOctoberInSkopje, _pets.Id, 600_000);

            Assert.True(result.IsSuccess);
            Assert.Empty(_limits.Added);
            Assert.Equal(600_000, october.LimitMinor);
            Assert.Equal(LastHourOfOctoberInSkopje, october.UpdatedAt);
        }

        [Fact]
        public async Task Execute_RowOfAnEarlierMonthOnly_KeepsItAndAddsARowForTheCurrentMonth()
        {
            BudgetLimit september = BudgetLimit.Create(CategoryTestData.OwnerId, null, new DateOnly(2026, 9, 1), 1_000_000, LastHourOfOctoberInSkopje.AddDays(-40));
            _limits.Seed(september);

            Result result = await ExecuteAsync(LastHourOfOctoberInSkopje, null, 800_000);

            Assert.True(result.IsSuccess);
            Assert.Equal(1_000_000, september.LimitMinor);
            Assert.Null(september.UpdatedAt);
            BudgetLimit added = Assert.Single(_limits.Added);
            Assert.Equal(new DateOnly(2026, 10, 1), added.EffectiveFrom);
            Assert.Equal(800_000, added.LimitMinor);
        }

        [Fact]
        public async Task Execute_CategoryOfAnotherPerson_FailsWith404CategoryNotFoundAndAddsNothing()
        {
            Result result = await new SetBudgetLimit(_limits, _categories, new FixedTimeProvider(LastHourOfOctoberInSkopje))
                .Execute(CategoryTestData.OtherOwnerId, Skopje, _pets.Id, 800_000, TestContext.Current.CancellationToken);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
            Assert.Empty(_limits.Added);
        }

        [Fact]
        public async Task Execute_UnknownCategoryWithABadAmount_FailsWith404CategoryNotFoundFirst()
        {
            Result result = await ExecuteAsync(LastHourOfOctoberInSkopje, Guid.CreateVersion7(), 0);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
            Assert.Empty(_limits.Added);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0L)]
        [InlineData(150L)]
        public async Task Execute_BadAmount_FailsWith400BudgetLimitAmountInvalidAndChangesNothing(long? limitMinor)
        {
            BudgetLimit october = BudgetLimit.Create(CategoryTestData.OwnerId, null, new DateOnly(2026, 10, 1), 800_000, LastHourOfOctoberInSkopje.AddDays(-5));
            _limits.Seed(october);

            Result result = await ExecuteAsync(LastHourOfOctoberInSkopje, null, limitMinor);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.BUDGET_LIMIT_AMOUNT_INVALID);
            Assert.Empty(_limits.Added);
            Assert.Equal(800_000, october.LimitMinor);
            Assert.Null(october.UpdatedAt);
        }

        private Task<Result> ExecuteAsync(DateTimeOffset now, Guid? categoryId, long? limitMinor)
        {
            return new SetBudgetLimit(_limits, _categories, new FixedTimeProvider(now))
                .Execute(CategoryTestData.OwnerId, Skopje, categoryId, limitMinor, TestContext.Current.CancellationToken);
        }
    }
}
