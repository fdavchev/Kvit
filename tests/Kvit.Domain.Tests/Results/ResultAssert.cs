using System.Net;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Domain.Tests.Results
{
    public static class ResultAssert
    {
        public static void Failed(Result result, HttpStatusCode expectedStatusCode, string expectedErrorCode)
        {
            Assert.False(result.IsSuccess);
            Assert.Equal(expectedStatusCode, result.StatusCode);
            Assert.Equal(expectedErrorCode, result.ErrorCode);
            Assert.NotEmpty(result.Error);
        }
    }
}
