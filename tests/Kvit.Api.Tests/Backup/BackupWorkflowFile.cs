using System.Text.RegularExpressions;
using Kvit.Api.Tests.Cleanup;

namespace Kvit.Api.Tests.Backup
{
    public static class BackupWorkflowFile
    {
        public const string RelativePath = ".github/workflows/backup.yml";

        private static readonly Regex StepStart = new(@"^ {6}- ");
        private static readonly Regex NameLine = new(@"^\s*(-\s+)?name\s*:");

        public static string ReadWithoutCommentLines()
        {
            return CleanupWorkflowFile.ReadWithoutCommentLines(RelativePath);
        }

        public static List<string> StepsOf(string text)
        {
            string jobs = CleanupWorkflowFile.BlockOf(text, "jobs");
            List<string> steps = [];
            List<string> current = [];
            foreach (string line in jobs.Split('\n'))
            {
                if (StepStart.IsMatch(line) && current.Count > 0)
                {
                    steps.Add(string.Join('\n', current));
                    current = [];
                }

                if (current.Count > 0 || StepStart.IsMatch(line))
                {
                    current.Add(line);
                }
            }

            if (current.Count > 0)
            {
                steps.Add(string.Join('\n', current));
            }

            if (steps.Count == 0)
            {
                throw new InvalidOperationException("No steps found: the steps must be list items indented by 6 spaces, as in ci.yml.");
            }

            return steps;
        }

        public static WorkflowCommand? FirstCommandMatching(string text, string pattern)
        {
            return CommandsMatching(text, pattern).FirstOrDefault();
        }

        public static List<WorkflowCommand> CommandsMatching(string text, string pattern)
        {
            Regex regex = new(pattern);
            string[] lines = text.Split('\n');
            List<WorkflowCommand> commands = [];
            int lineStart = 0;
            for (int index = 0; index < lines.Length; index++)
            {
                Match match = regex.Match(lines[index]);
                if (match.Success && !NameLine.IsMatch(lines[index]) && !lines[index].Contains("::error::", StringComparison.Ordinal))
                {
                    commands.Add(new WorkflowCommand(lineStart + match.Index, JoinContinuationLines(lines, index, match.Index)));
                }

                lineStart += lines[index].Length + 1;
            }

            return commands;
        }

        public static string UnquotedFileNameAfter(string command, string optionPattern)
        {
            Match match = Regex.Match(command, optionPattern + @"\s*(?<file>\S+)");
            if (!match.Success)
            {
                throw new InvalidOperationException($"The command has no match for '{optionPattern}': {command}");
            }

            return match.Groups["file"].Value.Trim('"', '\'');
        }

        private static string JoinContinuationLines(string[] lines, int firstLine, int firstColumn)
        {
            List<string> parts = [lines[firstLine][firstColumn..]];
            int index = firstLine;
            while (lines[index].TrimEnd().EndsWith('\\') && index + 1 < lines.Length)
            {
                index++;
                parts.Add(lines[index]);
            }

            return string.Join(' ', parts.Select(part => part.Trim().TrimEnd('\\').Trim()));
        }
    }
}
