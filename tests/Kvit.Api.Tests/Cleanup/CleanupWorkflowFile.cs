using System.Text.RegularExpressions;
using Kvit.Api.Tests.Scripts;

namespace Kvit.Api.Tests.Cleanup
{
    public static class CleanupWorkflowFile
    {
        public const string RelativePath = ".github/workflows/cleanup.yml";

        public static string ReadWithoutCommentLines()
        {
            return ReadWithoutCommentLines(RelativePath);
        }

        public static string ReadWithoutCommentLines(string relativePath)
        {
            string repositoryRoot = DotnetScript.FindRepositoryRoot();
            string path = Path.Combine(repositoryRoot, relativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"The workflow does not exist yet: {relativePath} (looked in {repositoryRoot}).");
            }

            IEnumerable<string> lines = File.ReadAllLines(path).Where(line => !line.TrimStart().StartsWith('#'));

            return string.Join('\n', lines);
        }

        public static string BlockOf(string text, string key)
        {
            List<string> lines = [.. text.Split('\n')];
            int start = lines.FindIndex(line => Regex.IsMatch(line, $@"^[""']?{Regex.Escape(key)}[""']?\s*:"));
            if (start < 0)
            {
                throw new InvalidOperationException($"The workflow has no top-level key '{key}'.");
            }

            List<string> block = [];
            for (int index = start + 1; index < lines.Count; index++)
            {
                string line = lines[index];
                if (line.Length > 0 && !char.IsWhiteSpace(line[0]))
                {
                    break;
                }

                block.Add(line);
            }

            return string.Join('\n', block);
        }

        public static List<string> NamesAtIndentOfTwo(string block)
        {
            return [.. Regex.Matches(block, @"^ {2}([A-Za-z0-9_-]+)\s*:", RegexOptions.Multiline).Select(match => match.Groups[1].Value)];
        }
    }
}
