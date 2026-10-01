using Kvit.Contracts.Me;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Contracts.Auth
{
    public interface IAccountService
    {
        Task<Result<MeResponse>> CreateAccountAsync(NewAccount account, CancellationToken cancellationToken);

        Task<Result<MeResponse>> CheckLogInAsync(string email, string password, string timeZone, CancellationToken cancellationToken);

        Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

        Task SignInAsync(Guid userId);

        Task LogOutAsync();

        Task<Result> ChangeLanguageAsync(Guid userId, string language, CancellationToken cancellationToken);
    }
}
