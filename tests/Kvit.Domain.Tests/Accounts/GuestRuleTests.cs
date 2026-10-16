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
    }
}
