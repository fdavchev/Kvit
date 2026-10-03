using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Me;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Me
{
    public sealed class GetMeQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetMeQuery, MeResponse>
    {
        public async Task<Result<MeResponse>> Handle(GetMeQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<MeResponse>(userId.Error, userId.ErrorCode);
            }

            MeResponse? me = await MeResponseOf(userId.Value).SingleOrDefaultAsync(cancellationToken);
            if (me is null)
            {
                return Result.Unauthorized<MeResponse>($"The signed-in account {userId.Value} no longer exists.", ResultCodes.AUTH_NOT_SIGNED_IN);
            }

            return Result.Ok(me);
        }

        private IQueryable<MeResponse> MeResponseOf(Guid userId)
        {
            return _context.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new MeResponse(user.Id, user.DisplayName, user.Email!, user.Language, user.TimeZone, user.MustChangePassword, user.PasswordHash != null));
        }
    }
}
