using Kvit.Contracts.Auth;

namespace Kvit.Api.Tests.Auth
{
    public static class GoogleIdentities
    {
        public const string PictureUrl = "https://example.com/pictures/ana.png";

        public static GoogleIdentity Verified()
        {
            return new GoogleIdentity($"sub-{Guid.NewGuid():N}", RegistrationForm.UniqueEmail(), true, "Ana Google", PictureUrl);
        }
    }
}
