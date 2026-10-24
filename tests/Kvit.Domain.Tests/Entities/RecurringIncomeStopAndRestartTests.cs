using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class RecurringIncomeStopAndRestartTests
    {
        private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        [Fact]
        public void Stop_ActiveIncome_StoresTheStopMoment()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();

            income.Stop(RecurringIncomeTestData.Moment.AddHours(2));

            Assert.Equal(RecurringIncomeTestData.Moment.AddHours(2), income.StoppedAt);
        }

        [Fact]
        public void Stop_AnAlreadyStoppedIncome_DoesNothingAndKeepsTheFirstStopMoment()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();
            income.Stop(RecurringIncomeTestData.Moment.AddHours(2));

            income.Stop(RecurringIncomeTestData.Moment.AddDays(1));

            Assert.Equal(RecurringIncomeTestData.Moment.AddHours(2), income.StoppedAt);
        }

        [Fact]
        public void Stop_ActiveIncome_KeepsEveryOtherField()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(endsOn: "2027-03-31"));

            income.Stop(RecurringIncomeTestData.Moment);

            Assert.Equal(RecurringIncomeTestData.Salary, income.AmountMinor);
            Assert.Equal("Salary", income.Title);
            Assert.Equal(15, income.DayOfMonth);
            Assert.Equal(new DateOnly(2026, 10, 1), income.StartsOn);
            Assert.Equal(new DateOnly(2027, 3, 31), income.EndsOn);
        }

        [Fact]
        public void Restart_OneSecondBeforeFiveDaysSinceTheStop_ClearsTheStopMoment()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();
            income.Stop(RecurringIncomeTestData.Moment);

            Result result = income.Restart(RecurringIncomeTestData.Moment + FiveDays - OneSecond);

            Assert.True(result.IsSuccess);
            Assert.Null(income.StoppedAt);
        }

        [Fact]
        public void Restart_RightAfterTheStop_ClearsTheStopMoment()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();
            income.Stop(RecurringIncomeTestData.Moment);

            Result result = income.Restart(RecurringIncomeTestData.Moment);

            Assert.True(result.IsSuccess);
            Assert.Null(income.StoppedAt);
        }

        [Fact]
        public void Restart_ExactlyFiveDaysSinceTheStop_FailsWithRestartExpiredAndStaysStopped()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();
            income.Stop(RecurringIncomeTestData.Moment);

            Result result = income.Restart(RecurringIncomeTestData.Moment + FiveDays);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_RESTART_EXPIRED);
            Assert.Equal(RecurringIncomeTestData.Moment, income.StoppedAt);
        }

        [Fact]
        public void Restart_MoreThanFiveDaysSinceTheStop_FailsWithRestartExpired()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();
            income.Stop(RecurringIncomeTestData.Moment);

            Result result = income.Restart(RecurringIncomeTestData.Moment + FiveDays + OneSecond);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_RESTART_EXPIRED);
        }

        [Fact]
        public void Restart_AnIncomeThatIsNotStopped_FailsWithNotStopped()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();

            Result result = income.Restart(RecurringIncomeTestData.Moment);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_NOT_STOPPED);
            Assert.Null(income.StoppedAt);
        }

        [Fact]
        public void Restart_ThenStopAgain_StoresTheSecondStopMoment()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();
            income.Stop(RecurringIncomeTestData.Moment);
            income.Restart(RecurringIncomeTestData.Moment.AddHours(1));

            income.Stop(RecurringIncomeTestData.Moment.AddDays(3));

            Assert.Equal(RecurringIncomeTestData.Moment.AddDays(3), income.StoppedAt);
        }
    }
}
