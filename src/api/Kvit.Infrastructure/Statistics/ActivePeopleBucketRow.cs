using System.ComponentModel.DataAnnotations.Schema;

namespace Kvit.Infrastructure.Statistics
{
    public sealed class ActivePeopleBucketRow
    {
        [Column("bucket_start")]
        public DateOnly BucketStart { get; init; }

        [Column("people")]
        public int People { get; init; }

        [Column("person_days")]
        public int PersonDays { get; init; }
    }
}
