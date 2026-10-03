using Google.Apis.Auth;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Newtonsoft.Json;

namespace Kvit.Infrastructure.Auth
{
    public sealed class GoogleTokenChecker(string _clientId) : IGoogleTokenChecker
    {
        public async Task<Result<GoogleIdentity>> CheckAsync(string idToken, CancellationToken cancellationToken)
        {
            GoogleJsonWebSignature.ValidationSettings settings = new() { Audience = [_clientId] };

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch (Exception exception) when (exception is InvalidJwtException or ArgumentException or FormatException or JsonReaderException)
            {
                return Result.Unauthorized<GoogleIdentity>($"The Google sign-in token is not valid: {exception.Message}", ResultCodes.AUTH_GOOGLE_TOKEN_INVALID);
            }

            return Result.Ok(new GoogleIdentity(
                RequiredClaim(payload.Subject, "sub"),
                RequiredClaim(payload.Email, "email"),
                payload.EmailVerified,
                RequiredClaim(payload.Name, "name"),
                payload.Picture));
        }

        private static string RequiredClaim(string? value, string claimName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"A Google token that passed the signature and audience checks has no '{claimName}' claim. " +
                    "The Google button must ask for the openid, email and profile scopes.");
            }

            return value;
        }
    }
}
