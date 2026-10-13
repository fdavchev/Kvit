using Kvit.Domain.Entities;

namespace Kvit.Infrastructure.Persistence
{
    public static class SettlementQueries
    {
        public static IQueryable<NamedSettlement> NamedSettlements(this AppDbContext context, Guid groupId)
        {
            IQueryable<NamedMember> members = context.NamedGroupMembers(groupId);

            return context.Settlements
                .Where(settlement => settlement.GroupId == groupId && settlement.DeletedAt == null)
                .Join(members, settlement => settlement.FromMemberId, from => from.Member.Id, (settlement, from) => new { Settlement = settlement, FromName = from.DisplayName })
                .Join(members, row => row.Settlement.ToMemberId, to => to.Member.Id, (row, to) => new NamedSettlement
                {
                    Settlement = row.Settlement,
                    FromName = row.FromName,
                    ToName = to.DisplayName,
                });
        }

        public static IQueryable<Guid> MemberIdsInAnySettlementOf(this AppDbContext context, Guid groupId)
        {
            IQueryable<Settlement> settlementsOfGroup = context.Settlements.Where(settlement => settlement.GroupId == groupId);

            return settlementsOfGroup
                .Select(settlement => settlement.FromMemberId)
                .Union(settlementsOfGroup.Select(settlement => settlement.ToMemberId));
        }

        public static IQueryable<Guid> MemberIdsInAnyExpenseOrSettlementOf(this AppDbContext context, Guid groupId)
        {
            return context.MemberIdsInAnyExpenseOf(groupId).Union(context.MemberIdsInAnySettlementOf(groupId));
        }
    }
}
