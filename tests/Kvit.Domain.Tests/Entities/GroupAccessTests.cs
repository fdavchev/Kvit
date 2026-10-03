using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupAccessTests
    {
        [Fact]
        public void CheckOwner_TheOwner_Succeeds()
        {
            Group group = GroupTestData.NewGroup();

            Result result = group.CheckOwner(GroupTestData.OwnerId);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void CheckOwner_SomeoneElse_FailsWith403GroupNotOwner()
        {
            Group group = GroupTestData.NewGroup();

            Result result = group.CheckOwner(GroupTestData.MemberId);

            ResultAssert.Failed(result, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public void NotFound_Answers404GroupNotFoundNamingTheGroupId()
        {
            Result<Group> result = Group.NotFound<Group>(GroupTestData.StrangerId);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Contains(GroupTestData.StrangerId.ToString(), result.Error, StringComparison.Ordinal);
        }
    }
}
