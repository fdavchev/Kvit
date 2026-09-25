using Kvit.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Controllers
{
    public class BaseControllerTests(WebApplicationFactory<Program> _factory) : IClassFixture<WebApplicationFactory<Program>>
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

        [Fact]
        public void Result_Failure_AnswersProblemDetailsWithStatusDetailAndErrorCode()
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            ResultTestController controller = CreateController(scope);

            ActionResult response = controller.Convert(Result.NotFound(ErrorMessage, ErrorCode));

            AssertProblem(response);
        }

        [Fact]
        public void Result_FailureWithValueType_AnswersProblemDetailsWithStatusDetailAndErrorCode()
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            ResultTestController controller = CreateController(scope);

            ActionResult<int> response = controller.Convert(Result.NotFound<int>(ErrorMessage, ErrorCode));

            Assert.NotNull(response.Result);
            AssertProblem(response.Result);
        }

        private static ResultTestController CreateController(IServiceScope scope)
        {
            DefaultHttpContext httpContext = new() { RequestServices = scope.ServiceProvider };
            return new ResultTestController { ControllerContext = new ControllerContext { HttpContext = httpContext } };
        }

        private static void AssertProblem(ActionResult response)
        {
            ObjectResult objectResult = Assert.IsType<ObjectResult>(response);
            ProblemDetails problem = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
            Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
            Assert.Equal(ErrorMessage, problem.Detail);
            Assert.Equal(ErrorCode, problem.Extensions["errorCode"]);
        }
    }
}
