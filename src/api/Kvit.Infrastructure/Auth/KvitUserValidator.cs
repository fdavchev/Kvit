using Microsoft.AspNetCore.Identity;

namespace Kvit.Infrastructure.Auth
{
    public sealed class KvitUserValidator(IdentityErrorDescriber errors) : UserValidator<AppUser>(errors)
    {
        public override Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user)
        {
            if (user.IsGuest)
            {
                return Task.FromResult(IdentityResult.Success);
            }

            return base.ValidateAsync(manager, user);
        }
    }
}
