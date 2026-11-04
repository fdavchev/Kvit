namespace Kvit.Contracts.Me
{
    public sealed record MyDataExportResponse(
        DateTimeOffset ExportedAt,
        string PrivacyNotice,
        ExportedProfile Profile,
        ExportedPersonalBudget PersonalBudget,
        IReadOnlyList<ExportedGroup> Groups);
}
