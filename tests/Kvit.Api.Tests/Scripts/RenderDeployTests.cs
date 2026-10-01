using System.Net;
using System.Text.Json;
using Xunit;

namespace Kvit.Api.Tests.Scripts
{
    public class RenderDeployTests
    {
        private const string ScriptPath = "scripts/RenderDeploy.cs";
        private const string ApiKeyVariable = "RENDER_API_KEY";
        private const string FakeApiKey = "rnd_fake-key-for-tests-0123456789";
        private const string CommitId = "0123abcd4567ef89";
        private const string PollSeconds = "0.2";
        private const string GenerousTimeoutMinutes = "1";
        private const string TinyTimeoutMinutes = "0.05";

        [Fact]
        public async Task Deploy_QueuedThenBuildingThenLive_ExitsWithZeroAfterTriggeringTheCommit()
        {
            await using FakeRenderServer server = await FakeRenderServer.StartAsync(HttpStatusCode.Created, "queued", "build_in_progress", "build_in_progress", "live");

            ScriptRun run = await RunDeployAsync(server, FakeApiKey, GenerousTimeoutMinutes, "--commit", CommitId);

            AssertExitCode(0, run);
            Assert.Contains($"deploy {FakeRenderServer.DeployId}: queued", run.Lines);
            Assert.Single(run.Lines, line => line == $"deploy {FakeRenderServer.DeployId}: build_in_progress");
            Assert.Contains($"deploy {FakeRenderServer.DeployId}: live", run.Lines);
            Assert.DoesNotContain(FakeApiKey, run.Output);
            RecordedRequest trigger = Assert.Single(server.Requests, request => request.Method == "POST");
            Assert.Equal(FakeRenderServer.DeploysPath, trigger.PathAndQuery);
            using JsonDocument triggerBody = JsonDocument.Parse(trigger.Body);
            Assert.Equal(CommitId, triggerBody.RootElement.GetProperty("commitId").GetString());
            Assert.Equal("do_not_clear", triggerBody.RootElement.GetProperty("clearCache").GetString());
            Assert.All(server.Requests, request => Assert.Equal($"Bearer {FakeApiKey}", request.Authorization));
        }

        [Theory]
        [InlineData("build_failed")]
        [InlineData("brand_new_status")]
        public async Task Deploy_ReachesAStatusThatIsNotLive_ExitsWithOneAndNamesTheStatus(string status)
        {
            await using FakeRenderServer server = await FakeRenderServer.StartAsync(HttpStatusCode.Created, "queued", status);

            ScriptRun run = await RunDeployAsync(server, FakeApiKey, GenerousTimeoutMinutes, "--commit", CommitId);

            AssertExitCode(1, run);
            Assert.Contains(status, run.Output);
            Assert.DoesNotContain(FakeApiKey, run.Output);
        }

        [Fact]
        public async Task Deploy_TriggerRejectedWith401_ExitsWithOneShowingTheStatusAndBodyButNotTheKey()
        {
            await using FakeRenderServer server = await FakeRenderServer.StartAsync(HttpStatusCode.Unauthorized);

            ScriptRun run = await RunDeployAsync(server, FakeApiKey, GenerousTimeoutMinutes, "--commit", CommitId);

            AssertExitCode(1, run);
            Assert.Contains("401", run.Output);
            Assert.Contains(FakeRenderServer.RejectionMessage, run.Output);
            Assert.DoesNotContain(FakeApiKey, run.Output);
        }

        [Fact]
        public async Task Deploy_TriggerAnswered202WithoutABody_ExitsWithOneSayingItWasQueuedAndPollsNothing()
        {
            await using FakeRenderServer server = await FakeRenderServer.StartAsync(HttpStatusCode.Accepted, "live");

            ScriptRun run = await RunDeployAsync(server, FakeApiKey, GenerousTimeoutMinutes, "--commit", CommitId);

            AssertExitCode(1, run);
            Assert.Contains(run.Lines, line => line.Contains("202") && line.Contains("queued", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(FakeApiKey, run.Output);
            Assert.DoesNotContain(server.Requests, request => request.Method == "GET");
        }

        [Fact]
        public async Task Deploy_NeverBecomesLive_ExitsWithOneSayingItTimedOutWithTheLastStatus()
        {
            await using FakeRenderServer server = await FakeRenderServer.StartAsync(HttpStatusCode.Created, "build_in_progress");

            ScriptRun run = await RunDeployAsync(server, FakeApiKey, TinyTimeoutMinutes, "--commit", CommitId);

            AssertExitCode(1, run);
            string timeoutLine = Assert.Single(run.Lines, line => line.Contains("timed out", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("build_in_progress", timeoutLine);
            Assert.DoesNotContain(FakeApiKey, run.Output);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Deploy_ApiKeyMissingOrEmpty_ExitsWithOneNamingTheVariableAndCallsNothing(string? apiKey)
        {
            await using FakeRenderServer server = await FakeRenderServer.StartAsync(HttpStatusCode.Created, "live");

            ScriptRun run = await RunDeployAsync(server, apiKey, GenerousTimeoutMinutes, "--commit", CommitId);

            AssertExitCode(1, run);
            Assert.Contains(ApiKeyVariable, run.Output);
            Assert.Empty(server.Requests);
        }

        [Fact]
        public async Task WatchLatest_LatestDeployBecomesLive_ExitsWithZeroAndTriggersNothing()
        {
            await using FakeRenderServer server = await FakeRenderServer.StartAsync(HttpStatusCode.Created, "build_in_progress", "live");

            ScriptRun run = await RunDeployAsync(server, FakeApiKey, GenerousTimeoutMinutes, "--watch-latest");

            AssertExitCode(0, run);
            Assert.Contains($"deploy {FakeRenderServer.DeployId}: live", run.Lines);
            Assert.Contains(server.Requests, request => request.Method == "GET" && request.PathAndQuery == $"{FakeRenderServer.DeploysPath}?limit=1");
            Assert.DoesNotContain(server.Requests, request => request.Method != "GET");
            Assert.All(server.Requests, request => Assert.Equal($"Bearer {FakeApiKey}", request.Authorization));
            Assert.DoesNotContain(FakeApiKey, run.Output);
        }

        private static Task<ScriptRun> RunDeployAsync(FakeRenderServer server, string? apiKey, string timeoutMinutes, params string[] modeArguments)
        {
            Dictionary<string, string?> environment = new() { [ApiKeyVariable] = apiKey };
            List<string> arguments =
            [
                "--service", FakeRenderServer.ServiceId,
                "--base-url", server.BaseUrl,
                "--poll-seconds", PollSeconds,
                "--timeout-minutes", timeoutMinutes,
                .. modeArguments,
            ];

            return DotnetScript.RunAsync(ScriptPath, environment, arguments);
        }

        private static void AssertExitCode(int expected, ScriptRun run)
        {
            Assert.True(run.ExitCode == expected, $"Expected exit code {expected} but got {run.ExitCode}. Output:\n{run.Output}");
        }
    }
}
