using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.BudgetLimits
{
    public class BudgetLimitAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [MemberData(nameof(BudgetLimitRequests.AllEndpoints), MemberType = typeof(BudgetLimitRequests))]
        public async Task Anonymous_Answers401(string endpoint)
        {
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await BudgetLimitRequests.SendAsync(anonymous, endpoint, BudgetLimitSteps.Food);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(BudgetLimitRequests.AllEndpoints), MemberType = typeof(BudgetLimitRequests))]
        public async Task Guest_Answers403GuestNotAllowedAndWritesNothing(string endpoint)
        {
            GuestMember guest = await JoinAsGuestAsync();
            string before = await BudgetLimitSteps.SnapshotAsync(_app);

            HttpResponseMessage response = await BudgetLimitRequests.SendAsync(guest.Client, endpoint, BudgetLimitSteps.Food);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.Equal(before, await BudgetLimitSteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Guest_SettingALimitOnAnUnknownCategory_Answers403BeforeLookingForTheCategory()
        {
            GuestMember guest = await JoinAsGuestAsync();

            HttpResponseMessage response = await BudgetLimitRequests.SetCategoryAsync(guest.Client, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
        }

        [Fact]
        public async Task Guest_SettingALimitWithABadAmount_Answers403BeforeCheckingTheAmount()
        {
            GuestMember guest = await JoinAsGuestAsync();

            HttpResponseMessage response = await BudgetLimitRequests.SetMonthAsync(guest.Client, 0);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
        }

        private async Task<GuestMember> JoinAsGuestAsync()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);

            return await GuestSteps.JoinWithNewNameAsync(_app, groupId, "Gost");
        }
    }
}
