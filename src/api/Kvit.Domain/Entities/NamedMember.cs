namespace Kvit.Domain.Entities
{
    public sealed record NamedMember
    {
        public required GroupMember Member { get; init; }

        public required string DisplayName { get; init; }

        public bool IsInAnyExpense { get; init; }
    }
}
