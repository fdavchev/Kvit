using Kvit.Application.Dispatching;
using Kvit.Contracts.Me;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Application.Queries.Me
{
    public static class FullAccount
    {
        public static async Task<Result<Guid>> IdOfCallerAsync(this IDispatcher dispatcher, CancellationToken cancellationToken)
        {
            Result<MeResponse> me = await dispatcher.FullAccountOfCallerAsync(cancellationToken);
            if (!me.IsSuccess)
            {
                return me.ToFailure<Guid>();
            }

            return Result.Ok(me.Value.Id);
        }

        public static async Task<Result<MeResponse>> FullAccountOfCallerAsync(this IDispatcher dispatcher, CancellationToken cancellationToken)
        {
            Result<MeResponse> me = await dispatcher.Query<GetMeQuery, MeResponse>(new GetMeQuery(), cancellationToken);
            if (!me.IsSuccess)
            {
                return me;
            }

            Result notGuest = AccountRules.CheckIsNotGuest(me.Value.IsGuest);
            if (!notGuest.IsSuccess)
            {
                return notGuest.ToFailure<MeResponse>();
            }

            return me;
        }
    }
}
