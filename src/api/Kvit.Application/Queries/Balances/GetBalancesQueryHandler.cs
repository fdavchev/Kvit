using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Balances;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Balances
{
    public sealed class GetBalancesQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetBalancesQuery, BalancesResponse>
    {
        public async Task<Result<BalancesResponse>> Handle(GetBalancesQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<BalancesResponse>(userId.Error, userId.ErrorCode);
            }

            Guid? ownerUserId = await _context.OwnerOfGroupForMember(query.GroupId, userId.Value).SingleOrDefaultAsync(cancellationToken);
            if (ownerUserId is null)
            {
                return Group.NotFound<BalancesResponse>(query.GroupId);
            }

            List<BalanceMemberDto> members = await BalanceMemberDtosOf(query.GroupId).ToListAsync(cancellationToken);
            List<BalanceExpense> expenses = await _context.CountedBalanceExpensesOfAsync(query.GroupId, cancellationToken);
            List<BalanceSettlement> settlements = await _context.CountedBalanceSettlementsOfAsync(query.GroupId, cancellationToken);
            BalanceSummary summary = Domain.MoneyRules.Balances.Calculate(
                [.. members.Select(member => member.MemberId)],
                expenses,
                settlements);
            List<CurrencyBalancesResponse> currencies = [.. summary.ByCurrency
                .OrderBy(currencyBalances => currencyBalances.Key)
                .Select(currencyBalances => CurrencyBalancesResponseOf(currencyBalances.Key, currencyBalances.Value, members))];

            return Result.Ok(new BalancesResponse(summary.IsEveryoneKvit, currencies));
        }

        private IQueryable<BalanceMemberDto> BalanceMemberDtosOf(Guid groupId)
        {
            return _context.NamedGroupMembers(groupId)
                .AsNoTracking()
                .Where(named => named.Member.EndKind != MemberEndKind.SetAside)
                .InJoiningOrder()
                .Select(named => new BalanceMemberDto(named.Member.Id, named.DisplayName, named.Member.RemovedAt == null));
        }

        private static CurrencyBalancesResponse CurrencyBalancesResponseOf(Currency currency, IReadOnlyList<MemberBalance> balances, IReadOnlyList<BalanceMemberDto> members)
        {
            if (balances.Count != members.Count)
            {
                throw new InvalidOperationException($"The {currency} balances have {balances.Count} rows but the group has {members.Count} counted members.");
            }

            Dictionary<Guid, BalanceMemberDto> membersById = members.ToDictionary(member => member.MemberId);
            List<MemberBalanceRow> rows = [.. balances.Select(balance =>
            {
                BalanceMemberDto member = membersById.GetValueOrDefault(balance.MemberId)
                    ?? throw new InvalidOperationException($"The {currency} balance of member {balance.MemberId} has no matching counted member in the group.");
                return new MemberBalanceRow(balance.MemberId, member.Name, balance.Balance.MinorUnits, member.IsCurrent);
            })];
            List<PaymentRow> payments = [.. DebtSimplifier.Simplify(balances).Select(payment => new PaymentRow(
                payment.FromMemberId,
                payment.ToMemberId,
                payment.Amount.MinorUnits))];

            return new CurrencyBalancesResponse(currency, rows, payments);
        }
    }
}
