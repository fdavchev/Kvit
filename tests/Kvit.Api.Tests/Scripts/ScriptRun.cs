namespace Kvit.Api.Tests.Scripts
{
    public sealed record ScriptRun(int ExitCode, string StandardOutput, string StandardError)
    {
        public string Output => StandardOutput + StandardError;

        public IReadOnlyList<string> Lines => Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
