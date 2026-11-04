using Kvit.Domain.Entities;

namespace Kvit.Domain.Statistics
{
    public sealed record UsageEventCount(UsageEventType Type, string? Detail, int Count);
}
