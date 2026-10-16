using System.Net;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Accounts
{
    public class GuestRuleTests
    {
        [Fact]
        public void CheckIsNotGuest_ForAGuest_FailsWith403GuestNotAllowed()
        {
            Result result = AccountRules.CheckIsNotGuest(isGuest: true);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
        }

        [Fact]
        public void CheckIsNotGuest_ForAPersonWithAFullAccount_Succeeds()
        {
            Result result = AccountRules.CheckIsNotGuest(isGuest: false);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void CheckIsGuest_ForAPersonWithAFullAccount_FailsWith409NotGuest()
        {
            Result result = AccountRules.CheckIsGuest(isGuest: false);

            ResultAssert.Failed(result, HttpStatusCode.Conflict, ResultCodes.AUTH_NOT_GUEST);
        }

        [Fact]
        public void CheckIsGuest_ForAGuest_Succeeds()
        {
            Result result = AccountRules.CheckIsGuest(isGuest: true);

            Assert.True(result.IsSuccess);
        }
    }
}
