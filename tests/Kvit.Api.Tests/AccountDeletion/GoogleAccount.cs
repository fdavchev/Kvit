using Kvit.Contracts.Auth;

namespace Kvit.Api.Tests.AccountDeletion
{
    public sealed record GoogleAccount(GoogleIdentity Identity, HttpClient Client, Guid UserId);
}
