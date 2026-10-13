namespace Kvit.Domain.Entities
{
    public sealed record NamedSettlement
    {
        public required Settlement Settlement { get; init; }

        public required string FromName { get; init; }

        public required string ToName { get; init; }
    }
}
