using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

namespace Kvit.Api.Wake
{
    public static class WakeReturnPath
    {
        public const int MaxLength = 2048;
        private const string Home = "/";

        public static string From(StringValues returnTo)
        {
            if (returnTo.Count != 1)
            {
                return Home;
            }

            string? path = returnTo[0];
            return IsLocalPath(path) ? path : Home;
        }

        private static bool IsLocalPath([NotNullWhen(true)] string? path)
        {
            return path is not null
                && path.Length <= MaxLength
                && path.StartsWith('/')
                && !path.Contains("//", StringComparison.Ordinal)
                && path.All(IsAllowedCharacter);
        }

        private static bool IsAllowedCharacter(char character)
        {
            return character is > ' ' and <= '~' and not '\\';
        }
    }
}
