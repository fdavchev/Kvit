namespace Kvit.Contracts.Admin
{
    public sealed record AdminStatsResponse(
        DateOnly From,
        DateOnly To,
        string By,
        DateOnly? PreviousFrom,
        DateOnly? PreviousTo,
        IReadOnlyList<DateOnly> Buckets,
        AdminAllTimeTotalsResponse AllTime,
        AdminSignUpStatsResponse SignUps,
        AdminActivePeopleStatsResponse ActivePeople,
        AdminEventStatsResponse JoinedViaInvite,
        AdminEventStatsResponse GroupsCreated,
        AdminEventStatsResponse ExpensesAdded,
        AdminEventStatsResponse PaymentsConfirmed);
}
