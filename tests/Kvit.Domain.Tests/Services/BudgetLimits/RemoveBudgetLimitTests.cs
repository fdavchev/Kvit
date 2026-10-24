using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.BudgetLimits;
using Kvit.Domain.Tests.Fakes;
using Xunit;

namespace Kvit.Domain.Tests.Services.BudgetLimits
{
    public class RemoveBudgetLimitTests
    {
        private const string Skopje = "Europe/Skopje";
        private static readonly Guid UserId = Guid.Parse("0199f0a1-b2c3-7d4e-8f5a-6b7c8d9e0f30");
        private static readonly Guid FoodId = Guid.Parse("c47e9a10-5f3b-4d2e-9a61-000000000001");
        private static readonly DateTimeOffset InOctober = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset FirstHourOfOctoberInSkopje = new(2026, 9, 30, 22, 30, 0, TimeSpan.Zero);

        private readonly FakeBudgetLimitRepository _limits = new();

        [Fact]
        public async Task Execute_NoRowsAtAll_ChangesNothing()
        {
            Result result = await ExecuteAsync(InOctober, FoodId);

            Assert.True(result.IsSuccess);
            Assert.Empty(_limits.Added);
            Assert.Empty(_limits.Removed);
        }

        [Fact]
        public async Task Execute_OnlyACurrentMonthRow_RemovesIt()
        {
            BudgetLimit october = Row(FoodId, new DateOnly(2026, 10, 1), 800_000);
            _limits.Seed(october);

            Result result = await ExecuteAsync(InOctober, FoodId);

            Assert.True(result.IsSuccess);
            Assert.Same(october, Assert.Single(_limits.Removed));
            Assert.Empty(_limits.Added);
        }

        [Fact]
        public async Task Execute_EarlierLimitInForceAndNoCurrentMonthRow_AddsANoLimitRowForTheCurrentMonthInTheUsersTimeZone()
        {
            BudgetLimit september = Row(null, new DateOnly(2026, 9, 1), 1_000_000);
            _limits.Seed(september);

            Result result = await ExecuteAsync(FirstHourOfOctoberInSkopje, null);

            Assert.True(result.IsSuccess);
            BudgetLimit added = Assert.Single(_limits.Added);
            Assert.Null(added.CategoryId);
            Assert.Equal(new DateOnly(2026, 10, 1), added.EffectiveFrom);
            Assert.Null(added.LimitMinor);
            Assert.Equal(1_000_000, september.LimitMinor);
            Assert.Empty(_limits.Removed);
        }

        [Fact]
        public async Task Execute_EarlierLimitInForceAndACurrentMonthRow_TurnsTheCurrentMonthRowIntoNoLimit()
        {
            BudgetLimit september = Row(FoodId, new DateOnly(2026, 9, 1), 1_000_000);
            BudgetLimit october = Row(FoodId, new DateOnly(2026, 10, 1), 800_000);
            _limits.Seed(september, october);

            Result result = await ExecuteAsync(InOctober, FoodId);

            Assert.True(result.IsSuccess);
            Assert.Null(october.LimitMinor);
            Assert.Equal(InOctober, october.UpdatedAt);
            Assert.Equal(1_000_000, september.LimitMinor);
            Assert.Empty(_limits.Added);
            Assert.Empty(_limits.Removed);
        }

        [Fact]
        public async Task Execute_LatestEarlierRowIsAlreadyNoLimit_RemovesTheCurrentMonthRowInsteadOfAddingAnotherNoLimitRow()
        {
            BudgetLimit august = Row(FoodId, new DateOnly(2026, 8, 1), 1_000_000);
            BudgetLimit september = Row(FoodId, new DateOnly(2026, 9, 1), null);
            BudgetLimit october = Row(FoodId, new DateOnly(2026, 10, 1), 600_000);
            _limits.Seed(august, september, october);

            Result result = await ExecuteAsync(InOctober, FoodId);

            Assert.True(result.IsSuccess);
            Assert.Same(october, Assert.Single(_limits.Removed));
            Assert.Empty(_limits.Added);
        }

        [Fact]
        public async Task Execute_RowsOfAnotherCategory_AreNotTouched()
        {
            BudgetLimit monthRow = Row(null, new DateOnly(2026, 10, 1), 800_000);
            _limits.Seed(monthRow);

            Result result = await ExecuteAsync(InOctober, FoodId);

            Assert.True(result.IsSuccess);
            Assert.Empty(_limits.Removed);
            Assert.Equal(800_000, monthRow.LimitMinor);
        }

        private static BudgetLimit Row(Guid? categoryId, DateOnly effectiveFrom, long? limitMinor)
        {
            return BudgetLimit.Create(UserId, categoryId, effectiveFrom, limitMinor, InOctober.AddDays(-60));
        }

        private Task<Result> ExecuteAsync(DateTimeOffset now, Guid? categoryId)
        {
            return new RemoveBudgetLimit(_limits, new FixedTimeProvider(now))
                .Execute(UserId, Skopje, categoryId, TestContext.Current.CancellationToken);
        }
    }
}
