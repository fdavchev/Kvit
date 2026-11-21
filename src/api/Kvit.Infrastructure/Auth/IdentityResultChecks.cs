using Kvit.Infrastructure.Persistence;
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

        public static async Task ThrowIfAccountChangedMeanwhileAsync(this AppDbContext context, IdentityResult saved, CancellationToken cancellationToken)
        {
            if (saved.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure)))
            {
                throw await context.DiscardBecauseAccountChangedMeanwhileAsync(Describe(saved), cancellationToken);
            }
        }

        public static string Describe(IdentityResult result)
        {
            return string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        }
    }
}
