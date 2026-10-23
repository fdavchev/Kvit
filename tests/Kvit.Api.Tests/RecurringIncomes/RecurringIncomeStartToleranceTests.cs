using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class RecurringIncomeStartToleranceTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string HalfPastMidnightOnOctober20InSkopje = "2026-10-19T22:30:00Z";

        [Fact]
        public async Task Create_InSkopjeJustAfterLocalMidnight_AcceptsLocalTodayPlusOneYear()
        {
            CatchUpSteps.SetNow(_app, HalfPastMidnightOnOctober20InSkopje);
            SignedInUser user = await CatchUpSteps.SignUpInAsync(_app, CatchUpSteps.Skopje);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(startsOn: "2027-10-20"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Create_InSkopjeJustAfterLocalMidnight_RefusesTheDayAfterLocalTodayPlusOneYear()
        {
            CatchUpSteps.SetNow(_app, HalfPastMidnightOnOctober20InSkopje);
            SignedInUser user = await CatchUpSteps.SignUpInAsync(_app, CatchUpSteps.Skopje);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(startsOn: "2027-10-21"));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_START_DATE_INVALID);
        }
    }
}
