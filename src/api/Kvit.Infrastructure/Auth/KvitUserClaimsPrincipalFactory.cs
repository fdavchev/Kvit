using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Kvit.Infrastructure.Auth
{
    public sealed class KvitUserClaimsPrincipalFactory(UserManager<AppUser> userManager, IOptions<IdentityOptions> optionsAccessor)
        : UserClaimsPrincipalFactory<AppUser>(userManager, optionsAccessor)
    {
        public const string MustChangePasswordClaimType = "kvit:must_change_password";

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
        {
            ClaimsIdentity identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new Claim(MustChangePasswordClaimType, user.MustChangePassword.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Boolean));

            return identity;
        }
    }
}
