using System.ComponentModel;
using System.Diagnostics;
using Xunit;

namespace Kvit.Api.Tests.Scripts
{
    public class BackupEnvScriptTests
    {
        private const string ScriptPath = "scripts/BackupEnv.cs";
        private const string ConnectionStringVariable = "NEON_DIRECT_CONNECTION_STRING";
        private const string Secret = "S3cr3t-Pa55";
        private const string NeonHost = "ep-cool-123.eu-central-1.aws.neon.tech";
        private const string NeonPoolerHost = "ep-cool-123-pooler.eu-central-1.aws.neon.tech";
        private const string TrickyPassword = "it's \"q\" $HOME `id` back\\slash & it's";
        private const string TrickyPasswordInConnectionString = "\"it's \"\"q\"\" $HOME `id` back\\slash & it's\"";
        private const string TrickyPasswordExported = "'it'\\''s \"q\" $HOME `id` back\\slash & it'\\''s'";

        private static readonly TimeSpan MaximumBashTime = TimeSpan.FromSeconds(30);

        private static readonly string NeonStyleConnectionString =
            $"Host={NeonHost};Database=neondb;Username=neondb_owner;Password={Secret};SSL Mode=VerifyFull;Channel Binding=Require";

        public static TheoryData<string> ConnectionStringsWithAliases =>
        [
            "Server=h1;Initial Catalog=d1;User Id=u1;Pwd=p1",
            "Host=h1;Database=d1;UserId=u1;Password=p1",
            "Host=h1;Database=d1;User=u1;Password=p1",
            "Host=h1;Database=d1;Username=u1;Password=p1",
        ];

        public static TheoryData<string, string> SslModes => new()
        {
            { "VerifyFull", "verify-full" },
            { "VerifyCA", "verify-ca" },
            { "Require", "require" },
            { "Prefer", "prefer" },
            { "Allow", "allow" },
            { "Disable", "disable" },
            { "verifyfull", "verify-full" },
            { "REQUIRE", "require" },
            { "disable", "disable" },
        };

        public static TheoryData<string, string> ChannelBindings => new()
        {
            { "Require", "require" },
            { "Prefer", "prefer" },
            { "Disable", "disable" },
            { "require", "require" },
            { "DISABLE", "disable" },
        };

        public static TheoryData<string?, string> FailingInputs => new()
        {
            { null, ConnectionStringVariable },
            { "", ConnectionStringVariable },
            { $"Database=d1;Username=u1;Password={Secret}", "Host" },
            { $"Host=h1;Username=u1;Password={Secret}", "Database" },
            { $"Host=h1;Database=d1;Password={Secret}", "Username" },
            { "Host=h1;Database=d1;Username=u1", "Password" },
            { $"Host=;Database=d1;Username=u1;Password={Secret}", "Host" },
            { "Host=h1;Database=d1;Username=u1;Password=", "Password" },
            { $"Host={NeonPoolerHost};Database=d1;Username=u1;Password={Secret}", "pooler" },
            { $"Host=h1;Database=d1;Username=u1;Password={Secret};SSL Mode=Banana", "Banana" },
            { $"Host=h1;Database=d1;Username=u1;Password={Secret};Port=abc", "Port" },
            { $"Host=h1;Database=d1;Username=u1;Password={Secret};Port=0", "Port" },
            { $"Host=h1;Database=d1;Username=u1;Password={Secret};Port=-1", "Port" },
            { $"Host=h1;Database=d1;Username=u1;Password={Secret};Port=65536", "Port" },
            { $"Host=h1;Database=d1;Username=u1;Password={Secret};Port=99999999999", "Port" },
        };

        [Fact]
        public async Task BackupEnv_NeonStyleConnectionString_PrintsTheSixExportLinesInOrder()
        {
            ScriptRun run = await RunAsync(NeonStyleConnectionString);

            Assert.True(run.ExitCode == 0, $"Expected exit code 0 but got {run.ExitCode}. Output:\n{run.Output}");
            Assert.Equal(
                [
                    $"export PGHOST='{NeonHost}'",
                    "export PGPORT='5432'",
                    "export PGDATABASE='neondb'",
                    "export PGUSER='neondb_owner'",
                    $"export PGPASSWORD='{Secret}'",
                    "export PGSSLMODE='verify-full'",
                    "export PGCHANNELBINDING='require'",
                ],
                OutputLines(run));
            Assert.Empty(run.StandardError);
        }

        [Fact]
        public async Task BackupEnv_NoPortNoSslModeNoChannelBinding_UsesTheDefaultsAndPrintsNoChannelBindingLine()
        {
            ScriptRun run = await RunAsync($"Host=h1;Database=d1;Username=u1;Password={Secret}");

            Assert.True(run.ExitCode == 0, $"Expected exit code 0 but got {run.ExitCode}. Output:\n{run.Output}");
            Assert.Equal(
                [
                    "export PGHOST='h1'",
                    "export PGPORT='5432'",
                    "export PGDATABASE='d1'",
                    "export PGUSER='u1'",
                    $"export PGPASSWORD='{Secret}'",
                    "export PGSSLMODE='require'",
                ],
                OutputLines(run));
        }

        [Fact]
        public async Task BackupEnv_PortGiven_PrintsThatPort()
        {
            ScriptRun run = await RunAsync($"Host=h1;Port=6543;Database=d1;Username=u1;Password={Secret}");

            Assert.True(run.ExitCode == 0, $"Expected exit code 0 but got {run.ExitCode}. Output:\n{run.Output}");
            Assert.Contains("export PGPORT='6543'", OutputLines(run));
        }

        [Theory]
        [MemberData(nameof(ConnectionStringsWithAliases))]
        public async Task BackupEnv_KeyAliases_AreUnderstood(string connectionString)
        {
            ScriptRun run = await RunAsync(connectionString);

            Assert.True(run.ExitCode == 0, $"Expected exit code 0 but got {run.ExitCode}. Output:\n{run.Output}");
            IReadOnlyList<string> lines = OutputLines(run);
            Assert.Contains("export PGHOST='h1'", lines);
            Assert.Contains("export PGDATABASE='d1'", lines);
            Assert.Contains("export PGUSER='u1'", lines);
            Assert.Contains("export PGPASSWORD='p1'", lines);
        }

        [Fact]
        public async Task BackupEnv_KeysAndValuesInAnyCase_AreUnderstood()
        {
            ScriptRun run = await RunAsync($"HOST=h1;PORT=5433;DATABASE=d1;USERNAME=u1;PASSWORD={Secret};SSL MODE=verifyfull;CHANNEL BINDING=require");

            Assert.True(run.ExitCode == 0, $"Expected exit code 0 but got {run.ExitCode}. Output:\n{run.Output}");
            Assert.Equal(
                [
                    "export PGHOST='h1'",
                    "export PGPORT='5433'",
                    "export PGDATABASE='d1'",
                    "export PGUSER='u1'",
                    $"export PGPASSWORD='{Secret}'",
                    "export PGSSLMODE='verify-full'",
                    "export PGCHANNELBINDING='require'",
                ],
                OutputLines(run));
        }

        [Theory]
        [MemberData(nameof(SslModes))]
        public async Task BackupEnv_SslMode_IsMappedToTheLibpqValue(string sslMode, string expected)
        {
            ScriptRun run = await RunAsync($"Host=h1;Database=d1;Username=u1;Password={Secret};SSL Mode={sslMode}");

            Assert.True(run.ExitCode == 0, $"Expected exit code 0 but got {run.ExitCode}. Output:\n{run.Output}");
            Assert.Contains($"export PGSSLMODE='{expected}'", OutputLines(run));
        }

        [Theory]
        [MemberData(nameof(ChannelBindings))]
        public async Task BackupEnv_ChannelBinding_IsMappedToTheLibpqValue(string channelBinding, string expected)
        {
            ScriptRun run = await RunAsync($"Host=h1;Database=d1;Username=u1;Password={Secret};Channel Binding={channelBinding}");

            Assert.True(run.ExitCode == 0, $"Expected exit code 0 but got {run.ExitCode}. Output:\n{run.Output}");
            Assert.Equal($"export PGCHANNELBINDING='{expected}'", OutputLines(run)[^1]);
        }

        [Fact]
        public async Task BackupEnv_QuotedValuesWithSemicolonAndEqualsSign_AreKeptWhole()
        {
            ScriptRun run = await RunAsync("Host=h1;Database='my;db';Username=u1;Password=\"a;b=c\"");

            Assert.True(run.ExitCode == 0, $"Expected exit code 0 but got {run.ExitCode}. Output:\n{run.Output}");
            IReadOnlyList<string> lines = OutputLines(run);
            Assert.Contains("export PGDATABASE='my;db'", lines);
            Assert.Contains("export PGPASSWORD='a;b=c'", lines);
        }

        [Fact]
        public async Task BackupEnv_PasswordWithQuotesDollarBackticksAndSpaces_IsWrittenInPosixSingleQuotes()
        {
            ScriptRun run = await RunAsync($"Host=h1;Database=d1;Username=u1;Password={TrickyPasswordInConnectionString}");

            Assert.True(run.ExitCode == 0, $"Expected exit code 0 but got {run.ExitCode}. Output:\n{run.Output}");
            Assert.Contains($"export PGPASSWORD={TrickyPasswordExported}", OutputLines(run));
        }

        [Fact]
        public async Task BackupEnv_PasswordWithQuotesDollarBackticksAndSpaces_ComesBackUnchangedWhenBashEvaluatesTheOutput()
        {
            ScriptRun run = await RunAsync($"Host=h1;Database=d1;Username=u1;Password={TrickyPasswordInConnectionString};SSL Mode=Prefer");
            Assert.True(run.ExitCode == 0, $"Expected exit code 0 but got {run.ExitCode}. Output:\n{run.Output}");
            string exports = string.Join('\n', OutputLines(run));

            ScriptRun? evaluated = await EvaluateWithBashAsync(exports);

            if (evaluated is null)
            {
                Assert.Contains($"export PGPASSWORD={TrickyPasswordExported}", OutputLines(run));
                return;
            }

            Assert.True(evaluated.ExitCode == 0, $"bash failed with exit code {evaluated.ExitCode} on:\n{exports}\nOutput:\n{evaluated.Output}");
            Assert.Equal($"h1\n5432\nd1\nu1\n{TrickyPassword}\nprefer", evaluated.StandardOutput);
        }

        [Fact]
        public async Task BackupEnv_SameInputTwice_GivesIdenticalOutput()
        {
            ScriptRun first = await RunAsync(NeonStyleConnectionString);
            ScriptRun second = await RunAsync(NeonStyleConnectionString);

            Assert.Equal(0, first.ExitCode);
            Assert.Equal(first.StandardOutput, second.StandardOutput);
        }

        [Theory]
        [MemberData(nameof(FailingInputs))]
        public async Task BackupEnv_UnusableConnectionString_ExitsWithTwoAndNamesTheProblemOnStandardErrorOnly(string? connectionString, string expectedMention)
        {
            ScriptRun run = await RunAsync(connectionString);

            Assert.True(run.ExitCode == 2, $"Expected exit code 2 but got {run.ExitCode}. Output:\n{run.Output}");
            Assert.Empty(run.StandardOutput);
            Assert.Contains(expectedMention, run.StandardError, StringComparison.OrdinalIgnoreCase);
            Assert.Single(run.StandardError.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            Assert.DoesNotContain(Secret, run.StandardError);
            if (!string.IsNullOrEmpty(connectionString))
            {
                Assert.DoesNotContain(connectionString, run.StandardError);
            }
        }

        [Fact]
        public async Task BackupEnv_PoolerHost_SaysThatTheDirectConnectionIsNeeded()
        {
            ScriptRun run = await RunAsync($"Host={NeonPoolerHost};Database=d1;Username=u1;Password={Secret}");

            Assert.True(run.ExitCode == 2, $"Expected exit code 2 but got {run.ExitCode}. Output:\n{run.Output}");
            Assert.Contains("direct", run.StandardError, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task BackupEnv_ConnectionStringThatCannotBeParsed_ExitsWithTwoWithoutLeakingThePassword()
        {
            ScriptRun run = await RunAsync($"Password={Secret};Host");

            Assert.True(run.ExitCode == 2, $"Expected exit code 2 but got {run.ExitCode}. Output:\n{run.Output}");
            Assert.Empty(run.StandardOutput);
            Assert.NotEmpty(run.StandardError.Trim());
            Assert.DoesNotContain(Secret, run.StandardError);
        }

        private static Task<ScriptRun> RunAsync(string? connectionString)
        {
            Dictionary<string, string?> environment = new() { [ConnectionStringVariable] = connectionString };

            return DotnetScript.RunAsync(ScriptPath, environment, []);
        }

        private static IReadOnlyList<string> OutputLines(ScriptRun run)
        {
            return run.StandardOutput.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
        }

        private static async Task<ScriptRun?> EvaluateWithBashAsync(string exports)
        {
            ScriptRun? probe = await RunBashAsync("printf ok");
            if (probe is null || probe.ExitCode != 0 || probe.StandardOutput != "ok")
            {
                return null;
            }

            string script = exports.ReplaceLineEndings("\n") +
                "\nprintf '%s\\n%s\\n%s\\n%s\\n%s\\n%s' \"$PGHOST\" \"$PGPORT\" \"$PGDATABASE\" \"$PGUSER\" \"$PGPASSWORD\" \"$PGSSLMODE\"\n";

            return await RunBashAsync(script);
        }

        private static async Task<ScriptRun?> RunBashAsync(string script)
        {
            ProcessStartInfo startInfo = new("bash")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add("-s");

            Process? process;
            try
            {
                process = Process.Start(startInfo);
            }
            catch (Win32Exception)
            {
                return null;
            }

            if (process is null)
            {
                return null;
            }

            using (process)
            {
                await process.StandardInput.WriteAsync(script);
                process.StandardInput.Close();
                Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
                Task<string> standardError = process.StandardError.ReadToEndAsync();

                using CancellationTokenSource limit = new(MaximumBashTime);
                try
                {
                    await process.WaitForExitAsync(limit.Token);
                }
                catch (OperationCanceledException)
                {
                    process.Kill(entireProcessTree: true);
                    throw new TimeoutException($"bash did not finish within {MaximumBashTime.TotalSeconds} seconds.");
                }

                return new ScriptRun(process.ExitCode, await standardOutput, await standardError);
            }
        }
    }
}
