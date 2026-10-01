using Microsoft.AspNetCore.Identity;

namespace Kvit.Infrastructure.Auth
{
    internal static class IdentityResultChecks
    {
        public static void ThrowIfFailed(IdentityResult result, string attempt)
        {
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Identity refused to {attempt}: {Describe(result)}");
            }
        }

        public static string Describe(IdentityResult result)
        {
            return string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        }
    }
}
