using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupWorkflowTests
    {
        private const string CleanupUrl = "https://kvit-mk.pages.dev/api/internal/cleanup";
        private const string SecretReference = "${{ secrets.CLEANUP_SECRET }}";
        private const int MinimumMaxTimeSeconds = 120;
        private const int MinimumRetries = 2;

        public static TheoryData<string> ForbiddenLiterals =>
        [
            "Host=",
            "Username=",
            "Password=",
            "postgres://",
            "postgresql://",
            "KvitDatabase",
            "Bearer ",
        ];

        [Fact]
        public void Workflow_ExistsAtTheDocumentedPath()
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            Assert.NotEmpty(text);
        }

        [Fact]
        public void Triggers_AreOnlyScheduleAndManualRun()
        {
            string triggers = CleanupWorkflowFile.BlockOf(CleanupWorkflowFile.ReadWithoutCommentLines(), "on");

            string[] names = [.. CleanupWorkflowFile.NamesAtIndentOfTwo(triggers).Order(StringComparer.Ordinal)];
            Assert.Equal(["schedule", "workflow_dispatch"], names);
        }

        [Fact]
        public void Schedule_HasExactlyOneCron()
        {
            string triggers = CleanupWorkflowFile.BlockOf(CleanupWorkflowFile.ReadWithoutCommentLines(), "on");

            Assert.Single(Regex.Matches(triggers, @"-\s*cron\s*:"));
        }

        [Fact]
        public void Schedule_RunsOncePerDayAtAFixedTime()
        {
            string triggers = CleanupWorkflowFile.BlockOf(CleanupWorkflowFile.ReadWithoutCommentLines(), "on");

            Match cron = Regex.Match(triggers, @"cron\s*:\s*[""']?(\d{1,2}) (\d{1,2}) \* \* \*[""']?\s*$", RegexOptions.Multiline);

            Assert.True(cron.Success, $"The cron is not 'minute hour * * *' with a fixed minute and hour:\n{triggers}");
            Assert.InRange(int.Parse(cron.Groups[1].Value, CultureInfo.InvariantCulture), 0, 59);
            Assert.InRange(int.Parse(cron.Groups[2].Value, CultureInfo.InvariantCulture), 0, 23);
        }

        [Fact]
        public void Triggers_CanBeStartedByHand()
        {
            string triggers = CleanupWorkflowFile.BlockOf(CleanupWorkflowFile.ReadWithoutCommentLines(), "on");

            Assert.Contains("workflow_dispatch", CleanupWorkflowFile.NamesAtIndentOfTwo(triggers));
        }

        [Fact]
        public void Triggers_DoNotRunOnPush()
        {
            string triggers = CleanupWorkflowFile.BlockOf(CleanupWorkflowFile.ReadWithoutCommentLines(), "on");

            Assert.DoesNotContain("push", CleanupWorkflowFile.NamesAtIndentOfTwo(triggers));
        }

        [Fact]
        public void Workflow_NeverMentionsPullRequestTriggers()
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            Assert.DoesNotContain("pull_request", text);
        }

        [Fact]
        public void Permissions_AreContentsReadAndNothingElse()
        {
            string permissions = CleanupWorkflowFile.BlockOf(CleanupWorkflowFile.ReadWithoutCommentLines(), "permissions");

            string[] lines = [.. permissions.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0)];
            Assert.Equal(["contents: read"], lines);
        }

        [Fact]
        public void Jobs_AreExactlyOne()
        {
            string jobs = CleanupWorkflowFile.BlockOf(CleanupWorkflowFile.ReadWithoutCommentLines(), "jobs");

            Assert.Single(CleanupWorkflowFile.NamesAtIndentOfTwo(jobs));
        }

        [Fact]
        public void Call_PostsToThePublicSiteAddress()
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            Assert.Contains(CleanupUrl, text);
            Assert.Matches(@"(-X|--request)\s+POST\b", text);
        }

        [Fact]
        public void Call_SendsTheSecretInTheCleanupHeader()
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            Assert.Matches(@"X-Kvit-Cleanup-Secret:\s*(\$\{\{\s*secrets\.CLEANUP_SECRET\s*\}\}|\$CLEANUP_SECRET|\$\{CLEANUP_SECRET\})", text);
        }

        [Fact]
        public void Call_TakesTheSecretFromTheRepositorySecretNamedCleanupSecret()
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            Assert.Contains(SecretReference, text);
        }

        [Fact]
        public void Workflow_UsesNoOtherSecret()
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            string[] secretNames = [.. Regex.Matches(text, @"secrets\.([A-Za-z0-9_]+)").Select(match => match.Groups[1].Value).Distinct()];
            Assert.Equal(["CLEANUP_SECRET"], secretNames);
        }

        [Fact]
        public void Call_FailsTheJobOnAnyAnswerThatIsNotSuccessful()
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            Assert.Matches(@"--fail(-with-body)?(\s|\\|$)", text);
        }

        [Fact]
        public void Call_WaitsLongEnoughForTheFreeServerToWakeUp()
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            Match maxTime = Regex.Match(text, @"--max-time\s+(\d+)");

            Assert.True(maxTime.Success, "The curl call has no --max-time.");
            Assert.True(
                int.Parse(maxTime.Groups[1].Value, CultureInfo.InvariantCulture) >= MinimumMaxTimeSeconds,
                $"--max-time is {maxTime.Groups[1].Value} seconds, less than {MinimumMaxTimeSeconds}.");
        }

        [Fact]
        public void Call_RetriesMoreThanOnce()
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            Match retry = Regex.Match(text, @"--retry\s+(\d+)");

            Assert.True(retry.Success, "The curl call has no --retry.");
            Assert.True(
                int.Parse(retry.Groups[1].Value, CultureInfo.InvariantCulture) >= MinimumRetries,
                $"--retry is {retry.Groups[1].Value}, less than {MinimumRetries}.");
        }

        [Fact]
        public void SecretCheck_HappensBeforeTheCall()
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            Match check = Regex.Match(text, @"-z\s+""?\$\{?CLEANUP_SECRET\}?""?");

            Assert.True(check.Success, "No step tests whether CLEANUP_SECRET is empty with -z.");
            Assert.True(check.Index < text.IndexOf("curl", StringComparison.Ordinal), "The empty-secret check comes after the curl call.");
        }

        [Fact]
        public void SecretCheck_FailsTheJobWithAClearMessageNamingTheSecret()
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            Assert.Matches(@"::error::.*CLEANUP_SECRET", text);
            Assert.Matches(@"exit 1", text);
        }

        [Fact]
        public void Workflow_DoesNotHideFailures()
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            Assert.DoesNotContain("continue-on-error", text);
            Assert.DoesNotContain("|| true", text);
            Assert.DoesNotContain("set +e", text);
        }

        [Theory]
        [MemberData(nameof(ForbiddenLiterals))]
        public void Workflow_ContainsNoConnectionStringOrSecretLiteral(string forbidden)
        {
            string text = CleanupWorkflowFile.ReadWithoutCommentLines();

            Assert.DoesNotContain(forbidden, text);
        }
    }
}
