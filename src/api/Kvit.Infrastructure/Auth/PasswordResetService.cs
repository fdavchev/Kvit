using System.Security.Cryptography;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Kvit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kvit.Infrastructure.Auth
{
    public sealed class PasswordResetService(
        UserManager<AppUser> _userManager,
        AppDbContext _context,
        IUnitOfWork _unitOfWork)
    {
        private const int TemporaryPasswordLength = 12;
        private const string TemporaryPasswordCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

        public Task<string?> ResetAsync(string email, CancellationToken cancellationToken)
        {
            return AccountWriteRetry.OnceMoreIfAccountChangedMeanwhileAsync(() => ResetOnceAsync(email, cancellationToken));
        }

        private async Task<string?> ResetOnceAsync(string email, CancellationToken cancellationToken)
        {
            AppUser? user = await _userManager.FindByEmailAsync(email.Trim());
            if (user is null)
            {
                return null;
            }

            string temporaryPassword = NewTemporaryPassword();
            user.MustChangePassword = true;
            user.LockoutCount = 0;
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;

            await _unitOfWork.OpenTransactionAsync(cancellationToken);
            IdentityResult removed = await _userManager.RemovePasswordAsync(user);
            await _context.ThrowIfAccountChangedMeanwhileAsync(removed, cancellationToken);
            IdentityResultChecks.ThrowIfFailed(removed, $"remove the password of user {user.Id}");
            IdentityResult added = await _userManager.AddPasswordAsync(user, temporaryPassword);
            await _context.ThrowIfAccountChangedMeanwhileAsync(added, cancellationToken);
            IdentityResultChecks.ThrowIfFailed(added, $"set the temporary password of user {user.Id}");
            await _unitOfWork.CommitAsync(cancellationToken);

            return temporaryPassword;
        }

        private static string NewTemporaryPassword()
        {
            string candidate;
            do
            {
                candidate = RandomNumberGenerator.GetString(TemporaryPasswordCharacters, TemporaryPasswordLength);
            }
            while (!AccountRules.ValidatePassword(candidate).IsSuccess);

            return candidate;
        }
    }
}
