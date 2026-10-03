namespace Kvit.Domain.Entities
{
    public sealed record GroupChanges(FieldChange? Name, IReadOnlyList<FieldChange> Settings);
}
