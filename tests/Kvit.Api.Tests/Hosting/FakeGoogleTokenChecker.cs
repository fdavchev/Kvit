using System.Collections.Concurrent;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;

namespace Kvit.Api.Tests.Hosting
{
    public sealed class FakeGoogleTokenChecker : IGoogleTokenChecker
    {
        private readonly ConcurrentDictionary<string, GoogleIdentity> _identities = new();

        public void Add(string token, GoogleIdentity identity)
        {
            _identities[token] = identity;
        }

        public Task<Result<GoogleIdentity>> CheckAsync(string idToken, CancellationToken cancellationToken)
        {
            if (_identities.TryGetValue(idToken, out GoogleIdentity? identity))
            {
                return Task.FromResult(Result.Ok(identity));
            }

            return Task.FromResult(Result.Unauthorized<GoogleIdentity>($"The fake Google checker does not know the token '{idToken}'.", ResultCodes.AUTH_GOOGLE_TOKEN_INVALID));
        }
    }
}
