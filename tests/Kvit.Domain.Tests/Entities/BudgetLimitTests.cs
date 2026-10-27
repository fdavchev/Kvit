using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class BudgetLimitTests
    {
        private static readonly Guid UserId = Guid.Parse("0199f0a1-b2c3-7d4e-8f5a-6b7c8d9e0f20");
        private static readonly DateTimeOffset Moment = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

        [Theory]
        [InlineData(100)]
        [InlineData(800_000)]
        [InlineData(999_999_999_900)]
        public void CheckAmount_PositiveWholeDenarsWithinTheBound_ReturnsTheAmount(long limitMinor)
        {
            Result<long> result = BudgetLimit.CheckAmount(limitMinor);

            Assert.True(result.IsSuccess);
            Assert.Equal(limitMinor, result.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0L)]
        [InlineData(-100L)]
        [InlineData(1L)]
        [InlineData(150L)]
        [InlineData(999_999_999_999L)]
        [InlineData(1_000_000_000_000L)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        public void CheckAmount_NoWholeNumberOrNotAPositiveWholeDenarWithinTheBound_FailsWithTheBudgetLimitCode(long? limitMinor)
        {
            Result<long> result = BudgetLimit.CheckAmount(limitMinor);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.BUDGET_LIMIT_AMOUNT_INVALID);
        }

        [Theory]
        [InlineData(2026, 10, 1, 2026, 10, 1)]
        [InlineData(2026, 10, 31, 2026, 10, 1)]
        [InlineData(2028, 2, 29, 2028, 2, 1)]
        public void FirstDayOfMonthOf_AnyDay_IsTheFirstDayOfItsMonth(int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
        {
            DateOnly firstDay = BudgetLimit.FirstDayOfMonthOf(new DateOnly(year, month, day));

            Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), firstDay);
        }

        [Fact]
        public void Create_EffectiveFromThatIsNotTheFirstDay_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => BudgetLimit.Create(UserId, null, new DateOnly(2026, 10, 2), 800_000, Moment));
        }

        [Fact]
        public void Create_RowWithoutALimit_KeepsEveryFieldAndHasNoChangeMoment()
        {
            BudgetLimit limit = BudgetLimit.Create(UserId, null, new DateOnly(2026, 10, 1), null, Moment);

            Assert.Equal(UserId, limit.UserId);
            Assert.Null(limit.CategoryId);
            Assert.Equal(new DateOnly(2026, 10, 1), limit.EffectiveFrom);
            Assert.Null(limit.LimitMinor);
            Assert.Equal(Moment, limit.CreatedAt);
            Assert.Null(limit.UpdatedAt);
        }

        [Fact]
        public void Change_ToAnotherValue_StoresItAndTheChangeMoment()
        {
            BudgetLimit limit = BudgetLimit.Create(UserId, null, new DateOnly(2026, 10, 1), 800_000, Moment);

            limit.Change(600_000, Moment.AddMinutes(1));

            Assert.Equal(600_000, limit.LimitMinor);
            Assert.Equal(Moment.AddMinutes(1), limit.UpdatedAt);
        }

        [Fact]
        public void Change_ToTheSameValue_ChangesNothing()
        {
            BudgetLimit limit = BudgetLimit.Create(UserId, null, new DateOnly(2026, 10, 1), 800_000, Moment);

            limit.Change(800_000, Moment.AddMinutes(1));

            Assert.Equal(800_000, limit.LimitMinor);
            Assert.Null(limit.UpdatedAt);
        }
    }
}
