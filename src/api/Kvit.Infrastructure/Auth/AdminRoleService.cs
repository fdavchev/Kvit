using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Microsoft.AspNetCore.Identity;

namespace Kvit.Infrastructure.Auth
{
    public sealed class AdminRoleService(
        UserManager<AppUser> _userManager,
        RoleManager<IdentityRole<Guid>> _roleManager,
        IUnitOfWork _unitOfWork)
    {
        public async Task<bool> GrantAsync(string email, CancellationToken cancellationToken)
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
                IdentityResultChecks.ThrowIfFailed(await _userManager.AddToRoleAsync(user, Roles.Admin), $"give user {user.Id} the {Roles.Admin} role");
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return true;
        }

        public async Task<bool> RevokeAsync(string email, CancellationToken cancellationToken)
        {
            AppUser? user = await FindAccountAsync(email);
            if (user is null)
            {
                return false;
            }

            await _unitOfWork.OpenTransactionAsync(cancellationToken);
            if (await _userManager.IsInRoleAsync(user, Roles.Admin))
            {
                IdentityResultChecks.ThrowIfFailed(await _userManager.RemoveFromRoleAsync(user, Roles.Admin), $"take the {Roles.Admin} role from user {user.Id}");
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
