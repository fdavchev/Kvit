using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Kvit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Kvit.Infrastructure.Auth
{
    public sealed class AdminRoleService(
        UserManager<AppUser> _userManager,
        RoleManager<IdentityRole<Guid>> _roleManager,
        AppDbContext _context,
        IUnitOfWork _unitOfWork)
    {
        public Task<bool> GrantAsync(string email, CancellationToken cancellationToken)
        {
            return AccountWriteRetry.OnceMoreIfAccountChangedMeanwhileAsync(_unitOfWork, () => GrantOnceAsync(email, cancellationToken));
        }

        public Task<bool> RevokeAsync(string email, CancellationToken cancellationToken)
        {
            return AccountWriteRetry.OnceMoreIfAccountChangedMeanwhileAsync(_unitOfWork, () => RevokeOnceAsync(email, cancellationToken));
        }

        private async Task<bool> GrantOnceAsync(string email, CancellationToken cancellationToken)
        {
            AppUser? user = await FindAccountAsync(email);
            if (user is null)
            {
                return false;
            }

            await _unitOfWork.OpenTransactionAsync(cancellationToken);
            if (!await _roleManager.RoleExistsAsync(Roles.Admin))
            {
                IdentityResultChecks.ThrowIfFailed(await _roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Admin)), $"create the {Roles.Admin} role");
            }

            if (!await _userManager.IsInRoleAsync(user, Roles.Admin))
            {
                IdentityResult added = await _userManager.AddToRoleAsync(user, Roles.Admin);
                await _context.ThrowIfAccountChangedMeanwhileAsync(added, cancellationToken);
                IdentityResultChecks.ThrowIfFailed(added, $"give user {user.Id} the {Roles.Admin} role");
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return true;
        }

        private async Task<bool> RevokeOnceAsync(string email, CancellationToken cancellationToken)
        {
            AppUser? user = await FindAccountAsync(email);
            if (user is null)
            {
                return false;
            }

            await _unitOfWork.OpenTransactionAsync(cancellationToken);
            if (await _userManager.IsInRoleAsync(user, Roles.Admin))
            {
                IdentityResult removed = await _userManager.RemoveFromRoleAsync(user, Roles.Admin);
                await _context.ThrowIfAccountChangedMeanwhileAsync(removed, cancellationToken);
                IdentityResultChecks.ThrowIfFailed(removed, $"take the {Roles.Admin} role from user {user.Id}");
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return true;
        }

        private async Task<AppUser?> FindAccountAsync(string email)
        {
            string trimmed = email.Trim();
            if (trimmed.Length == 0)
            {
                return null;
            }

            return await _userManager.FindByEmailAsync(trimmed);
        }
    }
}
