using Kvit.Api.Tests.Hosting;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Controllers
{
    public class BaseControllerTests(KvitApiFactory _factory) : IClassFixture<KvitApiFactory>
    {
        private const string ErrorMessage = "Group was not found.";
        private const string ErrorCode = "TEST_NOT_FOUND";

        [Fact]
        public void Result_Ok_Answers204WithoutBody()
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            ResultTestController controller = CreateController(scope);

            ActionResult response = controller.Convert(Result.Ok());

            Assert.IsType<NoContentResult>(response);
        }

        [Fact]
        public void Result_OkWithValue_Answers200WithTheValue()
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            ResultTestController controller = CreateController(scope);

            ActionResult<int> response = controller.Convert(Result.Ok(42));

            OkObjectResult ok = Assert.IsType<OkObjectResult>(response.Result);
            Assert.Equal(42, ok.Value);
        }

        [Theory]
        [InlineData(StatusCodes.Status400BadRequest)]
        [InlineData(StatusCodes.Status401Unauthorized)]
        [InlineData(StatusCodes.Status403Forbidden)]
        [InlineData(StatusCodes.Status404NotFound)]
        public void Result_Failure_AnswersProblemDetailsWithStatusDetailAndErrorCode(int statusCode)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            ResultTestController controller = CreateController(scope);

            ActionResult response = controller.Convert(FailureFor(statusCode));

            AssertProblem(response, statusCode);
        }

        [Theory]
        [InlineData(StatusCodes.Status400BadRequest)]
        [InlineData(StatusCodes.Status401Unauthorized)]
        [InlineData(StatusCodes.Status403Forbidden)]
        [InlineData(StatusCodes.Status404NotFound)]
        public void Result_FailureWithValueType_AnswersProblemDetailsWithStatusDetailAndErrorCode(int statusCode)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            ResultTestController controller = CreateController(scope);

            ActionResult<int> response = controller.Convert(FailureFor<int>(statusCode));

            Assert.NotNull(response.Result);
            AssertProblem(response.Result, statusCode);
        }

        private static Result FailureFor(int statusCode) => statusCode switch
        {
            StatusCodes.Status400BadRequest => Result.Failure(ErrorMessage, ErrorCode),
            StatusCodes.Status401Unauthorized => Result.Unauthorized(ErrorMessage, ErrorCode),
            StatusCodes.Status403Forbidden => Result.Forbid(ErrorMessage, ErrorCode),
            StatusCodes.Status404NotFound => Result.NotFound(ErrorMessage, ErrorCode),
            _ => throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "No Result factory for this status code."),
        };

        private static Result<T> FailureFor<T>(int statusCode) => statusCode switch
        {
            StatusCodes.Status400BadRequest => Result.Failure<T>(ErrorMessage, ErrorCode),
            StatusCodes.Status401Unauthorized => Result.Unauthorized<T>(ErrorMessage, ErrorCode),
            StatusCodes.Status403Forbidden => Result.Forbid<T>(ErrorMessage, ErrorCode),
            StatusCodes.Status404NotFound => Result.NotFound<T>(ErrorMessage, ErrorCode),
            _ => throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "No Result factory for this status code."),
        };

        private static ResultTestController CreateController(IServiceScope scope)
        {
            DefaultHttpContext httpContext = new() { RequestServices = scope.ServiceProvider };
            return new ResultTestController { ControllerContext = new ControllerContext { HttpContext = httpContext } };
        }

        private static void AssertProblem(ActionResult response, int expectedStatusCode)
        {
            ObjectResult objectResult = Assert.IsType<ObjectResult>(response);
            ProblemDetails problem = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal(expectedStatusCode, objectResult.StatusCode);
            Assert.Equal(expectedStatusCode, problem.Status);
            Assert.Equal(ErrorMessage, problem.Detail);
            Assert.Equal(ErrorCode, problem.Extensions["errorCode"]);
        }
    }
}
