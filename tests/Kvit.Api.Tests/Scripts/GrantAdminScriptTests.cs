using Xunit;

namespace Kvit.Api.Tests.Scripts
{
    public class GrantAdminScriptTests
    {
        private const string ScriptPath = "scripts/GrantAdmin.cs";

        public static IEnumerable<object[]> ArgumentsWithoutAnEmail =>
        [
            [new string[0]],
            [new[] { "--revoke" }],
        ];

        [Theory]
        [MemberData(nameof(ArgumentsWithoutAnEmail))]
        public async Task GrantAdmin_NoEmail_ExitsWithOneAndPrintsTheUsage(string[] arguments)
        {
            ScriptRun run = await DotnetScript.RunAsync(ScriptPath, new Dictionary<string, string?>(), arguments);

            Assert.True(run.ExitCode == 1, $"Expected exit code 1 but got {run.ExitCode}. Output:\n{run.Output}");
            Assert.Contains("Usage", run.Output);
            Assert.Contains("GrantAdmin.cs", run.Output);
        }
    }
}
