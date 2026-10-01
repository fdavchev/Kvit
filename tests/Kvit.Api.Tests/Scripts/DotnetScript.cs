using System.Diagnostics;

namespace Kvit.Api.Tests.Scripts
{
    public static class DotnetScript
    {
        private const string RepositoryMarkerFile = "Kvit.slnx";

        private static readonly TimeSpan MaximumRunTime = TimeSpan.FromMinutes(3);

        public static async Task<ScriptRun> RunAsync(string scriptPath, IReadOnlyDictionary<string, string?> environment, IReadOnlyList<string> arguments)
        {
            string repositoryRoot = FindRepositoryRoot();
            if (!File.Exists(Path.Combine(repositoryRoot, scriptPath)))
            {
                throw new FileNotFoundException($"The script does not exist yet: {scriptPath} (looked in {repositoryRoot}).");
            }

            ProcessStartInfo startInfo = new("dotnet")
            {
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add(scriptPath);
            startInfo.ArgumentList.Add("--");
            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            foreach (KeyValuePair<string, string?> variable in environment)
            {
                if (variable.Value is null)
                {
                    startInfo.Environment.Remove(variable.Key);
                }
                else
                {
                    startInfo.Environment[variable.Key] = variable.Value;
                }
            }

            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start 'dotnet run'. Is the .NET SDK on the PATH?");
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
            Task<string> standardError = process.StandardError.ReadToEndAsync();

            using CancellationTokenSource limit = new(MaximumRunTime);
            try
            {
                await process.WaitForExitAsync(limit.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException($"'dotnet run {scriptPath}' did not finish within {MaximumRunTime.TotalMinutes} minutes.");
            }

            return new ScriptRun(process.ExitCode, await standardOutput + await standardError);
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, RepositoryMarkerFile)))
            {
                directory = directory.Parent;
            }

            return directory?.FullName
                ?? throw new DirectoryNotFoundException($"No parent folder of {AppContext.BaseDirectory} contains {RepositoryMarkerFile}.");
        }
    }
}
