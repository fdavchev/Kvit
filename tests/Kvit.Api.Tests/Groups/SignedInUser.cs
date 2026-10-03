using Kvit.Api.Tests.Auth;

namespace Kvit.Api.Tests.Groups
{
    public sealed record SignedInUser(HttpClient Client, Guid UserId, RegistrationForm Form);
}
