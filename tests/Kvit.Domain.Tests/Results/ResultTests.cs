using System.Net;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Domain.Tests.Results
{
    public class ResultTests
    {
        private const string ErrorMessage = "Something went wrong.";
        private const string ErrorCode = "TEST_ERROR";

        [Fact]
        public void Ok_ReturnsSuccessWithStatus200AndNoError()
        {
            Result result = Result.Ok();

            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Empty(result.Error);
            Assert.Empty(result.ErrorCode);
        }

        [Fact]
        public void OkWithValue_ReturnsSuccessCarryingTheValue()
        {
            Result<int> result = Result.Ok(42);

            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void Failure_ReturnsStatus400WithErrorAndCode()
        {
            Result result = Result.Failure(ErrorMessage, ErrorCode);

            AssertFailure(result, HttpStatusCode.BadRequest);
        }

        [Fact]
        public void Unauthorized_ReturnsStatus401WithErrorAndCode()
        {
            Result result = Result.Unauthorized(ErrorMessage, ErrorCode);

            AssertFailure(result, HttpStatusCode.Unauthorized);
        }

        [Fact]
        public void Forbid_ReturnsStatus403WithErrorAndCode()
        {
            Result result = Result.Forbid(ErrorMessage, ErrorCode);

            AssertFailure(result, HttpStatusCode.Forbidden);
        }

        [Fact]
        public void NotFound_ReturnsStatus404WithErrorAndCode()
        {
            Result result = Result.NotFound(ErrorMessage, ErrorCode);

            AssertFailure(result, HttpStatusCode.NotFound);
        }

        [Fact]
        public void FailureWithValueType_ReturnsStatus400WithErrorAndCode()
        {
            Result<int> result = Result.Failure<int>(ErrorMessage, ErrorCode);

            AssertFailure(result, HttpStatusCode.BadRequest);
        }

        [Fact]
        public void UnauthorizedWithValueType_ReturnsStatus401WithErrorAndCode()
        {
            Result<int> result = Result.Unauthorized<int>(ErrorMessage, ErrorCode);

            AssertFailure(result, HttpStatusCode.Unauthorized);
        }

        [Fact]
        public void ForbidWithValueType_ReturnsStatus403WithErrorAndCode()
        {
            Result<int> result = Result.Forbid<int>(ErrorMessage, ErrorCode);

            AssertFailure(result, HttpStatusCode.Forbidden);
        }

        [Fact]
        public void NotFoundWithValueType_ReturnsStatus404WithErrorAndCode()
        {
            Result<int> result = Result.NotFound<int>(ErrorMessage, ErrorCode);

            AssertFailure(result, HttpStatusCode.NotFound);
        }

        [Fact]
        public void Value_OnFailedResult_ThrowsNamingTheErrorCode()
        {
            Result<int> result = Result.NotFound<int>(ErrorMessage, ErrorCode);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => result.Value);

            Assert.Contains(ErrorCode, exception.Message);
        }

        private static void AssertFailure(Result result, HttpStatusCode expectedStatusCode)
        {
            Assert.False(result.IsSuccess);
            Assert.Equal(expectedStatusCode, result.StatusCode);
            Assert.Equal(ErrorMessage, result.Error);
            Assert.Equal(ErrorCode, result.ErrorCode);
        }
    }
}
