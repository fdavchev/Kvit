namespace Kvit.Api.Tests.AccountDeletion
{
    public sealed record StoredDeletion(DateTimeOffset? RequestedAt, string? NameChoice, DateTimeOffset? PurgedAt)
    {
        public static readonly StoredDeletion None = new(null, null, null);
    }
}
