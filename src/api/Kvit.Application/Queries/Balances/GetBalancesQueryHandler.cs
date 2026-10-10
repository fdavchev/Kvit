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
            List<BalanceExpenseDto> expenses = await BalanceExpenseDtosOf(query.GroupId).ToListAsync(cancellationToken);
            BalanceSummary summary = Domain.MoneyRules.Balances.Calculate(
                [.. members.Select(member => member.MemberId)],
                [.. expenses.Select(BalanceExpenseOf)],
                []);
            List<CurrencyBalancesResponse> currencies = [.. summary.ByCurrency
                .OrderBy(currencyBalances => currencyBalances.Key)
                .Select(currencyBalances => CurrencyBalancesResponseOf(currencyBalances.Key, currencyBalances.Value, members))];

            return Result.Ok(new BalancesResponse(summary.IsEveryoneKvit, currencies));
        }

        private IQueryable<BalanceMemberDto> BalanceMemberDtosOf(Guid groupId)
        {
            return _context.NamedGroupMembers(groupId)
                .AsNoTracking()
                .InJoiningOrder()
                .Select(named => new BalanceMemberDto(named.Member.Id, named.DisplayName, named.Member.RemovedAt == null));
        }

        private IQueryable<BalanceExpenseDto> BalanceExpenseDtosOf(Guid groupId)
        {
            return _context.Expenses
                .AsNoTracking()
                .Where(expense => expense.GroupId == groupId && expense.DeletedAt == null)
                .Select(expense => new BalanceExpenseDto(
                    expense.PaidByMemberId,
                    expense.AmountMinor,
                    expense.Currency,
                    expense.Shares.Select(share => new BalanceShareDto(share.MemberId, share.InputValue, share.ShareMinor)).ToList()));
        }

        private static BalanceExpense BalanceExpenseOf(BalanceExpenseDto expense)
        {
            List<SplitShare> shares = [.. expense.Shares.Select(share => new SplitShare(
                share.MemberId,
                share.InputValue,
                Money.Create(share.ShareMinor, expense.Currency).Value))];

            return new BalanceExpense(expense.PaidByMemberId, Money.Create(expense.AmountMinor, expense.Currency).Value, shares, false);
        }

        private static CurrencyBalancesResponse CurrencyBalancesResponseOf(Currency currency, IReadOnlyList<MemberBalance> balances, IReadOnlyList<BalanceMemberDto> members)
        {
            List<MemberBalanceRow> rows = [.. balances.Zip(members, (balance, member) => new MemberBalanceRow(
                balance.MemberId,
                member.Name,
                balance.Balance.MinorUnits,
                member.IsCurrent))];
            List<PaymentRow> payments = [.. DebtSimplifier.Simplify(balances).Select(payment => new PaymentRow(
                payment.FromMemberId,
                payment.ToMemberId,
                payment.Amount.MinorUnits))];

            return new CurrencyBalancesResponse(currency, rows, payments);
        }
    }
}
