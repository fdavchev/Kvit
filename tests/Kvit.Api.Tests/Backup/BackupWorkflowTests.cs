using System.Globalization;
using System.Text.RegularExpressions;
using Kvit.Api.Tests.Cleanup;
using Xunit;

namespace Kvit.Api.Tests.Backup
{
    public class BackupWorkflowTests
    {
        private const string ConnectionStringSecret = "NEON_DIRECT_CONNECTION_STRING";
        private const string PassphraseSecret = "BACKUP_PASSPHRASE";
        private const string PostgresClientPackage = "postgresql-client-18";
        private const string PgDumpPattern = @"(?<![\w./-])pg_dump(?![\w.-])";
        private const string OpensslPattern = @"openssl\s+enc\b";
        private const string DecryptOption = @"\s-d(\s|$)";
        private const int MinimumIterations = 600000;
        private const string PassphraseLengthCheckPattern = @"\[\s+""\$\{#" + PassphraseSecret + @"\}""\s+-lt\s+30\s+\]";
        private const string UploadAction = "actions/upload-artifact";

        public static TheoryData<string> ForbiddenLiterals =>
        [
            "continue-on-error",
            "|| true",
            "set +e",
            "set -x",
            "Host=",
            "Username=",
            "Password=",
            "postgres://",
            "postgresql://",
            "Bearer ",
            "echo \"$BACKUP_PASSPHRASE\"",
            "echo $BACKUP_PASSPHRASE",
            "echo \"${BACKUP_PASSPHRASE}\"",
            "-pass pass:",
            "-kfile",
        ];

        public static TheoryData<string> SecretNames =>
        [
            ConnectionStringSecret,
            PassphraseSecret,
        ];

        [Fact]
        public void Workflow_ExistsAtTheDocumentedPath()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.NotEmpty(text);
        }

        [Fact]
        public void Triggers_AreOnlyScheduleAndManualRun()
        {
            string triggers = CleanupWorkflowFile.BlockOf(BackupWorkflowFile.ReadWithoutCommentLines(), "on");

            string[] names = [.. CleanupWorkflowFile.NamesAtIndentOfTwo(triggers).Order(StringComparer.Ordinal)];
            Assert.Equal(["schedule", "workflow_dispatch"], names);
        }

        [Fact]
        public void Schedule_HasExactlyOneCron()
        {
            string triggers = CleanupWorkflowFile.BlockOf(BackupWorkflowFile.ReadWithoutCommentLines(), "on");

            Assert.Single(Regex.Matches(triggers, @"-\s*cron\s*:"));
        }

        [Fact]
        public void Schedule_RunsOncePerWeekAtAFixedTime()
        {
            string triggers = CleanupWorkflowFile.BlockOf(BackupWorkflowFile.ReadWithoutCommentLines(), "on");

            Match cron = Regex.Match(
                triggers,
                @"cron\s*:\s*[""']?(\d{1,2}) (\d{1,2}) \* \* ([0-7]|SUN|MON|TUE|WED|THU|FRI|SAT)[""']?\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            Assert.True(cron.Success, $"The cron is not 'minute hour * * weekday' with a fixed minute, hour and weekday:\n{triggers}");
            Assert.InRange(int.Parse(cron.Groups[1].Value, CultureInfo.InvariantCulture), 0, 59);
            Assert.InRange(int.Parse(cron.Groups[2].Value, CultureInfo.InvariantCulture), 0, 23);
        }

        [Fact]
        public void Triggers_CanBeStartedByHand()
        {
            string triggers = CleanupWorkflowFile.BlockOf(BackupWorkflowFile.ReadWithoutCommentLines(), "on");

            Assert.Contains("workflow_dispatch", CleanupWorkflowFile.NamesAtIndentOfTwo(triggers));
        }

        [Fact]
        public void Triggers_DoNotRunOnPush()
        {
            string triggers = CleanupWorkflowFile.BlockOf(BackupWorkflowFile.ReadWithoutCommentLines(), "on");

            Assert.DoesNotContain("push", CleanupWorkflowFile.NamesAtIndentOfTwo(triggers));
        }

        [Fact]
        public void Workflow_NeverMentionsPullRequestTriggers()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.DoesNotContain("pull_request", text);
        }

        [Fact]
        public void Permissions_AreContentsReadAndNothingElse()
        {
            string permissions = CleanupWorkflowFile.BlockOf(BackupWorkflowFile.ReadWithoutCommentLines(), "permissions");

            string[] lines = [.. permissions.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0)];
            Assert.Equal(["contents: read"], lines);
        }

        [Fact]
        public void Jobs_AreExactlyOne()
        {
            string jobs = CleanupWorkflowFile.BlockOf(BackupWorkflowFile.ReadWithoutCommentLines(), "jobs");

            Assert.Single(CleanupWorkflowFile.NamesAtIndentOfTwo(jobs));
        }

        [Fact]
        public void Job_RunsOnUbuntuLatest()
        {
            string jobs = CleanupWorkflowFile.BlockOf(BackupWorkflowFile.ReadWithoutCommentLines(), "jobs");

            Assert.Matches(@"(?m)^\s+runs-on\s*:\s*ubuntu-latest\s*$", jobs);
        }

        [Fact]
        public void Actions_AreUsedWithAMajorVersionTagLikeInTheCiWorkflow()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            string[] used = [.. Regex.Matches(text, @"(?m)^\s*-?\s*uses\s*:\s*(\S+)\s*$").Select(match => match.Groups[1].Value)];
            Assert.NotEmpty(used);
            Assert.All(used, action => Assert.Matches(@"^[\w.-]+/[\w.-]+@v\d+$", action));
        }

        [Fact]
        public void Job_ChecksOutTheRepository()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.Matches(@"uses\s*:\s*actions/checkout@v\d+", text);
        }

        [Fact]
        public void Job_SetsUpDotnetFromGlobalJson()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            string step = Assert.Single(BackupWorkflowFile.StepsOf(text), candidate => candidate.Contains("actions/setup-dotnet@", StringComparison.Ordinal));
            Assert.Matches(@"(?m)^\s+global-json-file\s*:\s*global\.json\s*$", step);
        }

        [Fact]
        public void PostgresClient_IsVersion18FromTheOfficialAptRepository()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.Matches(@"apt(-get)?\s+install\b[^\n]*" + PostgresClientPackage, text);
            Assert.Contains("apt.postgresql.org", text);
        }

        [Fact]
        public void PostgresClient_IsNeverInstalledWithoutAVersion()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.DoesNotMatch(@"postgresql-client(?![-\w])", text);
        }

        [Fact]
        public void PostgresClient_IsInstalledBeforeTheDump()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            WorkflowCommand dump = RequireCommand(text, PgDumpPattern);
            int installedAt = text.IndexOf(PostgresClientPackage, StringComparison.Ordinal);
            Assert.True(installedAt >= 0 && installedAt < dump.Index, "postgresql-client-18 is not installed before pg_dump runs.");
        }

        [Theory]
        [MemberData(nameof(SecretNames))]
        public void SecretCheck_FailsWithAnErrorMessageNamingTheMissingSecret(string secretName)
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.Matches(@"::error::[^\n]*\b" + secretName + @"\b", text);
        }

        [Fact]
        public void SecretCheck_NamesEachSecretInItsOwnErrorMessage()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            string[] errorLines = [.. text.Split('\n').Where(line => line.Contains("::error::", StringComparison.Ordinal))];
            Assert.Contains(errorLines, line => line.Contains(ConnectionStringSecret, StringComparison.Ordinal) && !line.Contains(PassphraseSecret, StringComparison.Ordinal));
            Assert.Contains(errorLines, line => line.Contains(PassphraseSecret, StringComparison.Ordinal) && !line.Contains(ConnectionStringSecret, StringComparison.Ordinal));
        }

        [Theory]
        [MemberData(nameof(SecretNames))]
        public void SecretCheck_TestsEachSecretForEmpty(string secretName)
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.Matches(@"-z\s+""?\$\{?" + secretName + @"\}?""?", text);
        }

        [Fact]
        public void SecretCheck_FailsTheJob()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.Matches(@"(?m)exit\s+(1|""?\$\{?\w+\}?""?)\s*$", text);
        }

        [Fact]
        public void SecretCheck_HappensBeforeAnyWorkIsDone()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            MatchCollection checks = Regex.Matches(text, @"-z\s+""?\$\{?(" + ConnectionStringSecret + "|" + PassphraseSecret + @")\}?""?");
            Assert.NotEmpty(checks);
            int lastCheck = checks.Max(match => match.Index);
            int[] workStarts =
            [
                text.IndexOf("actions/setup-dotnet@", StringComparison.Ordinal),
                text.IndexOf(PostgresClientPackage, StringComparison.Ordinal),
                text.IndexOf("scripts/BackupEnv.cs", StringComparison.Ordinal),
                RequireCommand(text, PgDumpPattern).Index,
                RequireEncryption(text).Index,
                text.IndexOf(UploadAction, StringComparison.Ordinal),
            ];
            Assert.All(workStarts, workStart => Assert.True(workStart > lastCheck, $"Work starts at character {workStart}, before the last secret check at {lastCheck}."));
        }

        [Fact]
        public void PassphraseLengthCheck_FailsWhenThePassphraseIsShorterThanThirtyCharacters()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            string step = Assert.Single(BackupWorkflowFile.StepsOf(text), candidate => Regex.IsMatch(candidate, PassphraseLengthCheckPattern));
            Assert.Matches(@"::error::[^\n]*\b" + PassphraseSecret + @"\b[^\n]*30 characters", step);
            Assert.Matches(@"(?m)^\s+exit\s+1\s*$", step);
        }

        [Fact]
        public void PassphraseLengthCheck_HappensBeforeTheDumpAndTheEncryption()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Match check = Regex.Match(text, PassphraseLengthCheckPattern);
            Assert.True(check.Success, "The workflow has no check that BACKUP_PASSPHRASE is at least 30 characters long.");
            Assert.True(check.Index < RequireCommand(text, PgDumpPattern).Index, "The passphrase length is checked after pg_dump runs.");
            Assert.True(check.Index < RequireEncryption(text).Index, "The passphrase length is checked after the encryption.");
        }

        [Fact]
        public void Workflow_UsesExactlyTheTwoBackupSecrets()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            string[] secretNames = [.. Regex.Matches(text, @"secrets\.([A-Za-z0-9_]+)").Select(match => match.Groups[1].Value).Distinct().Order(StringComparer.Ordinal)];
            Assert.Equal([PassphraseSecret, ConnectionStringSecret], secretNames);
        }

        [Fact]
        public void Secrets_AreOnlyHandedToStepsThroughEnvEntries()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            string[] lines = [.. text.Split('\n').Where(line => line.Contains("secrets.", StringComparison.Ordinal))];
            Assert.NotEmpty(lines);
            Assert.All(lines, line => Assert.Matches(@"^\s+(NEON_DIRECT_CONNECTION_STRING|BACKUP_PASSPHRASE)\s*:\s*\$\{\{\s*secrets\.\1\s*\}\}\s*$", line));
        }

        [Theory]
        [MemberData(nameof(SecretNames))]
        public void Secret_IsOnlyMentionedWhereItCannotLeak(string secretName)
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            string[] allowed =
            [
                @"^\s+" + secretName + @"\s*:\s*\$\{\{\s*secrets\." + secretName + @"\s*\}\}\s*$",
                @"-z\s+""\$\{?" + secretName + @"\}?""",
                @"::error::",
                @"-pass\s+env:" + secretName + @"(\s|\\|$)",
                @"""\$\{#" + secretName + @"\}""\s+-lt\s+\d+",
            ];
            string[] mentions = [.. text.Split('\n').Where(line => line.Contains(secretName, StringComparison.Ordinal))];
            Assert.NotEmpty(mentions);
            Assert.All(mentions, line => Assert.True(allowed.Any(pattern => Regex.IsMatch(line, pattern)), $"{secretName} is used in a way that could leak it: {line.Trim()}"));
        }

        [Fact]
        public void ConnectionSettings_AreBuiltWithTheRepositoryScript()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.NotNull(BackupWorkflowFile.FirstCommandMatching(text, @"dotnet\s+run\s+scripts/BackupEnv\.cs"));
        }

        [Fact]
        public void ConnectionSettings_AreNotEvaluatedStraightFromTheScriptOutput()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.DoesNotMatch(@"eval\s+""?\$\(\s*dotnet\s+run", text);
            Assert.Matches(@"\b(eval|source)\b", text);
        }

        [Fact]
        public void ConnectionSettings_AreReadBeforeTheDump()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            WorkflowCommand script = RequireCommand(text, @"dotnet\s+run\s+scripts/BackupEnv\.cs");
            WorkflowCommand dump = RequireCommand(text, PgDumpPattern);
            Assert.True(script.Index < dump.Index, "scripts/BackupEnv.cs runs after pg_dump.");
        }

        [Fact]
        public void Dump_UsesTheCustomFormat()
        {
            WorkflowCommand dump = RequireCommand(BackupWorkflowFile.ReadWithoutCommentLines(), PgDumpPattern);

            Assert.Matches(@"(--format[= ]custom|-Fc)(\s|$)", dump.Text);
        }

        [Fact]
        public void Dump_LeavesOutOwnersAndPrivileges()
        {
            WorkflowCommand dump = RequireCommand(BackupWorkflowFile.ReadWithoutCommentLines(), PgDumpPattern);

            Assert.Matches(@"--no-owner(\s|$)", dump.Text);
            Assert.Matches(@"--no-acl(\s|$)", dump.Text);
        }

        [Fact]
        public void Dump_IsWrittenToAFile()
        {
            WorkflowCommand dump = RequireCommand(BackupWorkflowFile.ReadWithoutCommentLines(), PgDumpPattern);

            Assert.Matches(@"(--file[= ]|-f\s|>\s*\S)", dump.Text);
        }

        [Fact]
        public void Encryption_UsesAes256CbcWithPbkdf2()
        {
            WorkflowCommand encryption = RequireEncryption(BackupWorkflowFile.ReadWithoutCommentLines());

            Assert.Matches(@"\s-aes-256-cbc(\s|$)", encryption.Text);
            Assert.Matches(@"\s-pbkdf2(\s|$)", encryption.Text);
        }

        [Fact]
        public void Encryption_UsesAtLeastSixHundredThousandIterations()
        {
            WorkflowCommand encryption = RequireEncryption(BackupWorkflowFile.ReadWithoutCommentLines());

            Match iterations = Regex.Match(encryption.Text, @"\s-iter\s+(\d+)");
            Assert.True(iterations.Success, $"The openssl command has no -iter: {encryption.Text}");
            Assert.True(
                long.Parse(iterations.Groups[1].Value, CultureInfo.InvariantCulture) >= MinimumIterations,
                $"-iter is {iterations.Groups[1].Value}, less than {MinimumIterations}.");
        }

        [Fact]
        public void Encryption_TakesThePassphraseFromTheEnvironment()
        {
            WorkflowCommand encryption = RequireEncryption(BackupWorkflowFile.ReadWithoutCommentLines());

            Assert.Matches(@"\s-pass\s+env:" + PassphraseSecret + @"(\s|$)", encryption.Text);
        }

        [Fact]
        public void Encryption_NeverPutsThePassphraseOnTheCommandLine()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            foreach (WorkflowCommand command in BackupWorkflowFile.CommandsMatching(text, OpensslPattern))
            {
                Assert.DoesNotMatch(@"\s-pass\s+(pass|stdin|fd|file):", command.Text);
                Assert.DoesNotMatch(@"\s-k\s", command.Text);
                Assert.DoesNotMatch(@"\s-kfile\s", command.Text);
                Assert.DoesNotContain("$" + PassphraseSecret, command.Text);
                Assert.DoesNotContain("${" + PassphraseSecret, command.Text);
            }
        }

        [Fact]
        public void Encryption_ReadsTheDumpAndWritesAnEncFile()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();
            string dumpFile = DumpFileName(text);

            WorkflowCommand encryption = RequireEncryption(text);

            Assert.Equal(dumpFile, BackupWorkflowFile.UnquotedFileNameAfter(encryption.Text, @"\s-in\b"));
            Assert.EndsWith(".enc", BackupWorkflowFile.UnquotedFileNameAfter(encryption.Text, @"\s-out\b"), StringComparison.Ordinal);
        }

        [Fact]
        public void Encryption_HappensAfterTheDump()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            WorkflowCommand dump = RequireCommand(text, PgDumpPattern);
            WorkflowCommand encryption = RequireEncryption(text);
            Assert.True(dump.Index < encryption.Index, "openssl runs before pg_dump.");
        }

        [Fact]
        public void PlainDump_IsDeletedAfterEncryptingAndBeforeTheUpload()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();
            string dumpFile = DumpFileName(text);
            WorkflowCommand encryption = RequireEncryption(text);
            int upload = text.IndexOf(UploadAction, StringComparison.Ordinal);

            List<WorkflowCommand> deletions = BackupWorkflowFile.CommandsMatching(text, @"(?<![\w./-])(rm|shred)\s");

            Assert.Contains(
                deletions,
                deletion => deletion.Index > encryption.Index
                    && deletion.Index < upload
                    && deletion.Text.Replace("\"", string.Empty, StringComparison.Ordinal).Replace("'", string.Empty, StringComparison.Ordinal).Contains(dumpFile, StringComparison.Ordinal));
        }

        [Fact]
        public void EncryptedFile_IsCheckedToBeNotEmptyBeforeTheUpload()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();
            WorkflowCommand encryption = RequireEncryption(text);
            int upload = text.IndexOf(UploadAction, StringComparison.Ordinal);

            MatchCollection checks = Regex.Matches(text, @"(\[\[?|test)\s+(!\s+)?-s\s");

            Assert.Contains(checks, check => check.Index > encryption.Index && check.Index < upload);
        }

        [Fact]
        public void EncryptedFile_IsCheckedToDecryptToTheCustomFormatMagicBeforeTheUpload()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();
            WorkflowCommand encryption = RequireEncryption(text);
            WorkflowCommand decryption = RequireDecryption(text);
            int upload = text.IndexOf(UploadAction, StringComparison.Ordinal);
            int magic = text.IndexOf("PGDMP", StringComparison.Ordinal);

            Assert.True(decryption.Index > encryption.Index && decryption.Index < upload, "The decryption check is not between the encryption and the upload.");
            Assert.True(magic > encryption.Index && magic < upload, "PGDMP is not checked between the encryption and the upload.");
            Assert.Matches(@"\s-pass\s+env:" + PassphraseSecret + @"(\s|$)", decryption.Text);
        }

        [Fact]
        public void EncryptedFile_IsDecryptedWithTheSameSettingsAsItWasEncrypted()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();
            WorkflowCommand encryption = RequireEncryption(text);
            WorkflowCommand decryption = RequireDecryption(text);

            Assert.Matches(@"\s-aes-256-cbc(\s|$)", decryption.Text);
            Assert.Matches(@"\s-pbkdf2(\s|$)", decryption.Text);
            Assert.Equal(Regex.Match(encryption.Text, @"\s-iter\s+(\d+)").Groups[1].Value, Regex.Match(decryption.Text, @"\s-iter\s+(\d+)").Groups[1].Value);
            Assert.Equal(
                BackupWorkflowFile.UnquotedFileNameAfter(encryption.Text, @"\s-out\b"),
                BackupWorkflowFile.UnquotedFileNameAfter(decryption.Text, @"\s-in\b"));
        }

        [Fact]
        public void BackupDate_IsComputedWithTheDateCommand()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.Matches(@"\bdate\b[^\n]*\+%Y", text);
        }

        [Fact]
        public void Upload_HappensExactlyOnceWithTheUploadArtifactAction()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.Single(Regex.Matches(text, @"uses\s*:\s*" + UploadAction + @"@v\d+"));
            Assert.Single(Regex.Matches(text, "upload-artifact"));
        }

        [Fact]
        public void Upload_KeepsTheFileForNinetyDays()
        {
            string withBlock = UploadWithBlock();

            Assert.Matches(@"(?m)^\s+retention-days\s*:\s*[""']?90[""']?\s*$", withBlock);
        }

        [Fact]
        public void Upload_FailsWhenThereIsNoFile()
        {
            string withBlock = UploadWithBlock();

            Assert.Matches(@"(?m)^\s+if-no-files-found\s*:\s*error\s*$", withBlock);
        }

        [Fact]
        public void Upload_NamesTheArtifactAfterTheBackupAndTheDate()
        {
            string withBlock = UploadWithBlock();

            Assert.Matches(@"(?m)^\s+name\s*:\s*[""']?kvit-backup-\S*\$\{\{", withBlock);
        }

        [Fact]
        public void Upload_PathIsOnlyOneEncryptedFile()
        {
            string withBlock = UploadWithBlock();

            Match path = Regex.Match(withBlock, @"(?m)^\s+path\s*:\s*(\S.*?)\s*$");
            Assert.True(path.Success, $"The upload step has no single-line path:\n{withBlock}");
            string value = path.Groups[1].Value.Trim('"', '\'');
            Assert.EndsWith(".enc", value, StringComparison.Ordinal);
            string withoutExpressions = Regex.Replace(value, @"\$\{\{.*?\}\}", string.Empty);
            Assert.DoesNotContain(" ", withoutExpressions);
            Assert.DoesNotContain("!", withoutExpressions);
        }

        [Fact]
        public void Upload_NeverUploadsThePlainDump()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();
            string dumpFile = DumpFileName(text);

            Match path = Regex.Match(UploadWithBlock(), @"(?m)^\s+path\s*:\s*(\S.*?)\s*$");

            Assert.True(path.Success);
            Assert.NotEqual(Path.GetFileName(dumpFile), Path.GetFileName(path.Groups[1].Value.Trim('"', '\'')));
        }

        [Theory]
        [MemberData(nameof(ForbiddenLiterals))]
        public void Workflow_ContainsNoHiddenFailureNoConnectionStringAndNoPassphraseLeak(string forbidden)
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();

            Assert.DoesNotContain(forbidden, text);
        }

        private static WorkflowCommand RequireCommand(string text, string pattern)
        {
            return BackupWorkflowFile.FirstCommandMatching(text, pattern)
                ?? throw new InvalidOperationException($"{BackupWorkflowFile.RelativePath} has no command matching '{pattern}'.");
        }

        private static WorkflowCommand RequireEncryption(string text)
        {
            return BackupWorkflowFile.CommandsMatching(text, OpensslPattern).FirstOrDefault(command => !Regex.IsMatch(command.Text, DecryptOption))
                ?? throw new InvalidOperationException($"{BackupWorkflowFile.RelativePath} has no 'openssl enc' command that encrypts.");
        }

        private static WorkflowCommand RequireDecryption(string text)
        {
            return BackupWorkflowFile.CommandsMatching(text, OpensslPattern).FirstOrDefault(command => Regex.IsMatch(command.Text, DecryptOption))
                ?? throw new InvalidOperationException($"{BackupWorkflowFile.RelativePath} has no 'openssl enc -d' command that decrypts.");
        }

        private static string DumpFileName(string text)
        {
            WorkflowCommand dump = RequireCommand(text, PgDumpPattern);
            Match redirect = Regex.Match(dump.Text, @">\s*(\S+)");

            return redirect.Success
                ? redirect.Groups[1].Value.Trim('"', '\'')
                : BackupWorkflowFile.UnquotedFileNameAfter(dump.Text, @"(--file[= ]|-f\s)");
        }

        private static string UploadWithBlock()
        {
            string text = BackupWorkflowFile.ReadWithoutCommentLines();
            string step = Assert.Single(BackupWorkflowFile.StepsOf(text), candidate => candidate.Contains(UploadAction, StringComparison.Ordinal));
            Match withLine = Regex.Match(step, @"(?m)^\s+with\s*:\s*$");
            Assert.True(withLine.Success, $"The upload step has no 'with:' block:\n{step}");

            return step[(withLine.Index + withLine.Length)..];
        }
    }
}
