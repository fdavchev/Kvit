namespace Kvit.Contracts.PersonalEntries
{
    public sealed record LimitRow(Guid? CategoryId, long LimitMinor, long SpentMinor, long OverMinor, long IncludedEurMinor);
}
