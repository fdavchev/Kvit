using System.ComponentModel.DataAnnotations.Schema;

namespace Kvit.Infrastructure.Statistics
{
    public sealed class FirstEventDayRow
    {
        [Column("first_day")]
        public DateOnly? FirstDay { get; init; }
    }
}
