using System.ComponentModel.DataAnnotations.Schema;

namespace Kvit.Infrastructure.Statistics
{
    public sealed class ActivePeopleRow
    {
        [Column("people")]
        public int People { get; init; }

        [Column("person_days")]
        public int PersonDays { get; init; }

        [Column("guests")]
        public int Guests { get; init; }
    }
}
