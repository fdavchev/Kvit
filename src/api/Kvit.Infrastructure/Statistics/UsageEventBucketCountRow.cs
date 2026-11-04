using System.ComponentModel.DataAnnotations.Schema;

namespace Kvit.Infrastructure.Statistics
{
    public sealed class UsageEventBucketCountRow
    {
        [Column("bucket_start")]
        public DateOnly BucketStart { get; init; }

        [Column("type")]
        public required string Type { get; init; }

        [Column("detail")]
        public string? Detail { get; init; }

        [Column("count")]
        public int Count { get; init; }
    }
}
