using Kvit.Api.Tests.Groups;

namespace Kvit.Api.Tests.Settlements
{
    public sealed record SettlementGroup(
        Guid GroupId,
        SignedInUser Owner,
        Guid OwnerRowId,
        SignedInUser Member,
        Guid MemberRowId,
        SignedInUser Third,
        Guid ThirdRowId,
        Guid MarkoRowId,
        Guid CvetaRowId)
    {
        public IReadOnlyList<Guid> RowIdsInJoiningOrder => [OwnerRowId, MemberRowId, MarkoRowId, CvetaRowId, ThirdRowId];

        public SignedInUser UserNamed(string name)
        {
            return name switch
            {
                GroupsApp.OwnerName => Owner,
                GroupsApp.MemberName => Member,
                SettlementSteps.ThirdName => Third,
                _ => throw new ArgumentException($"{name} is not a member with an account in the settlement test group."),
            };
        }

        public Guid RowNamed(string name)
        {
            return name switch
            {
                GroupsApp.OwnerName => OwnerRowId,
                GroupsApp.MemberName => MemberRowId,
                SettlementSteps.ThirdName => ThirdRowId,
                SettlementSteps.MarkoName => MarkoRowId,
                SettlementSteps.CvetaName => CvetaRowId,
                _ => throw new ArgumentException($"{name} is not a member of the settlement test group."),
            };
        }
    }
}
