namespace Kvit.Api.Tests.Scripts
{
    public sealed record ScriptRun(int ExitCode, string Output)
    {
        public IReadOnlyList<string> Lines => Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
