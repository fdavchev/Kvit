using System.Net;
using Kvit.Api.Problems;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Cleanup;
using Kvit.Api.Tests.Hosting;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kvit.Api.Tests.Problems
{
    public class UnexpectedExceptionTests(KvitApiFactory _factory) : IClassFixture<KvitApiFactory>
    {
        private readonly RecordingLoggerProvider _logs = new();

        [Fact]
        public async Task UnexpectedException_Answers500ProblemWithServerErrorCode()
        {
            HttpResponseMessage response = await CreateClient().GetAsync("/test-exceptions/crash", TestContext.Current.CancellationToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.InternalServerError, ResultCodes.SERVER_ERROR);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task UnexpectedException_NeverPutsTheExceptionTextInTheAnswer()
        {
            HttpResponseMessage response = await CreateClient().GetAsync("/test-exceptions/crash", TestContext.Current.CancellationToken);

            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain(ExceptionEndpointsTestController.SecretDetail, body, StringComparison.Ordinal);
            Assert.DoesNotContain("InvalidOperationException", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task UnexpectedException_IsLoggedAsAnErrorWithTheExceptionAndTheRequest()
        {
            await CreateClient().GetAsync("/test-exceptions/crash", TestContext.Current.CancellationToken);

            RecordedLogEntry entry = Assert.Single(_logs.EntriesOf<UnexpectedExceptionHandler>());
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.IsType<InvalidOperationException>(entry.Exception);
            Assert.Contains(ExceptionEndpointsTestController.SecretDetail, entry.Exception.Message, StringComparison.Ordinal);
            Assert.Contains("GET /test-exceptions/crash", entry.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("/test-exceptions/concurrency")]
        [InlineData("/test-exceptions/member-changed-twice")]
        [InlineData("/test-exceptions/raw-sql-deadlock")]
        [InlineData("/test-exceptions/bare-deadlock")]
        [InlineData("/test-exceptions/saving-deadlock")]
        public async Task SameMomentClashThatEscapesTheHandlers_Answers409ChangedMeanwhileWithoutTheExceptionText(string path)
        {
            HttpResponseMessage response = await CreateClient().PostAsync(path, null, TestContext.Current.CancellationToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Conflict, ResultCodes.CHANGED_MEANWHILE);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain(ExceptionEndpointsTestController.SecretDetail, body, StringComparison.Ordinal);
            RecordedLogEntry entry = Assert.Single(_logs.EntriesOf<UnexpectedExceptionHandler>());
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.NotNull(entry.Exception);
        }

        [Fact]
        public async Task PostgresFailureThatIsNotADeadlock_StillAnswers500ServerError()
        {
            HttpResponseMessage response = await CreateClient().PostAsync("/test-exceptions/other-postgres-failure", null, TestContext.Current.CancellationToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.InternalServerError, ResultCodes.SERVER_ERROR);
            RecordedLogEntry entry = Assert.Single(_logs.EntriesOf<UnexpectedExceptionHandler>());
            Assert.Equal(LogLevel.Error, entry.Level);
        }

        [Theory]
        [InlineData("/test-exceptions/too-large", HttpStatusCode.RequestEntityTooLarge, ResultCodes.REQUEST_TOO_LARGE)]
        [InlineData("/test-exceptions/unreadable", HttpStatusCode.BadRequest, ResultCodes.REQUEST_INVALID)]
        public async Task BadHttpRequest_AnswersItsOwnStatusWithAClearCodeAndIsLoggedAsAWarningNotAServerError(string path, HttpStatusCode expectedStatus, string expectedErrorCode)
        {
            HttpResponseMessage response = await CreateClient().PostAsync(path, null, TestContext.Current.CancellationToken);

            await ProblemResponse.AssertAsync(response, expectedStatus, expectedErrorCode);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain(ExceptionEndpointsTestController.SecretDetail, body, StringComparison.Ordinal);
            Assert.DoesNotContain(ResultCodes.SERVER_ERROR, body, StringComparison.Ordinal);
            RecordedLogEntry entry = Assert.Single(_logs.EntriesOf<UnexpectedExceptionHandler>());
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.IsType<BadHttpRequestException>(entry.Exception);
        }

        [Fact]
        public void Codes_AreTheValuesTheWebsiteTranslates()
        {
            Assert.Equal("SERVER_ERROR", ResultCodes.SERVER_ERROR);
            Assert.Equal("CHANGED_MEANWHILE", ResultCodes.CHANGED_MEANWHILE);
            Assert.Equal("REQUEST_TOO_LARGE", ResultCodes.REQUEST_TOO_LARGE);
            Assert.Equal("REQUEST_INVALID", ResultCodes.REQUEST_INVALID);
        }

        private HttpClient CreateClient()
        {
            return _factory
                .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                {
                    services.AddControllers().AddApplicationPart(typeof(ExceptionEndpointsTestController).Assembly);
                    services.AddSingleton<ILoggerProvider>(_logs);
                }))
                .CreateClient();
        }
    }
}
