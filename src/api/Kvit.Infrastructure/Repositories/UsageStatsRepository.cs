using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Statistics;
using Kvit.Infrastructure.Persistence;
using Kvit.Infrastructure.Statistics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class UsageStatsRepository(AppDbContext _context) : IUsageStatsRepository
    {
        private const string CountedEvents = """
            WITH counted_events AS (
                SELECT usage_events.user_id, usage_events.type, usage_events.detail,
                    (usage_events.occurred_at AT TIME ZONE @time_zone)::date AS day
                FROM usage_events
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM user_roles
                    JOIN roles ON roles.id = user_roles.role_id
                    WHERE user_roles.user_id = usage_events.user_id AND roles.name = @admin_role))
            """;

        private const string ActivePersonDays = """
            , active_person_days AS (
                SELECT DISTINCT user_id, day
                FROM counted_events
                WHERE type = @active AND user_id IS NOT NULL AND day BETWEEN @from AND @to)
            """;

        private const string FirstEventDaySql = $"""
            {CountedEvents}
            SELECT min(day) AS first_day
            FROM counted_events
            """;

        private const string AllTimeSql = $"""
            {CountedEvents}
            SELECT type, detail, count(*)::int AS count
            FROM counted_events
            GROUP BY type, detail
            """;

        private const string PerBucketSql = $"""
            {CountedEvents}
            SELECT date_trunc(@grouping, day::timestamp)::date AS bucket_start, type, detail, count(*)::int AS count
            FROM counted_events
            WHERE day BETWEEN @from AND @to
            GROUP BY bucket_start, type, detail
            """;

        private const string ActivePeopleSql = $"""
            {CountedEvents}
            {ActivePersonDays}
            SELECT count(DISTINCT active_person_days.user_id)::int AS people,
                count(*)::int AS person_days,
                count(DISTINCT active_person_days.user_id) FILTER (WHERE users.is_guest)::int AS guests
            FROM active_person_days
            JOIN users ON users.id = active_person_days.user_id
            """;

        private const string ActivePeoplePerBucketSql = $"""
            {CountedEvents}
            {ActivePersonDays}
            SELECT date_trunc(@grouping, day::timestamp)::date AS bucket_start,
                count(DISTINCT user_id)::int AS people,
                count(*)::int AS person_days
            FROM active_person_days
            GROUP BY bucket_start
            """;

        public async Task<DateOnly?> FirstEventDayAsync(CancellationToken cancellationToken)
        {
            List<FirstEventDayRow> rows = await _context.Database
                .SqlQueryRaw<FirstEventDayRow>(FirstEventDaySql, ParametersWith())
                .ToListAsync(cancellationToken);

            return rows.Single().FirstDay;
        }

        public async Task<IReadOnlyList<UsageEventCount>> CountAllTimeAsync(CancellationToken cancellationToken)
        {
            List<UsageEventCountRow> rows = await _context.Database
                .SqlQueryRaw<UsageEventCountRow>(AllTimeSql, ParametersWith())
                .ToListAsync(cancellationToken);

            return [.. rows.Select(row => UsageEventCountOf(row.Type, row.Detail, row.Count))];
        }

        public async Task<IReadOnlyList<UsageEventBucketCount>> CountPerBucketAsync(StatsDateRange range, StatsGrouping grouping, CancellationToken cancellationToken)
        {
            List<UsageEventBucketCountRow> rows = await _context.Database
                .SqlQueryRaw<UsageEventBucketCountRow>(PerBucketSql, ParametersWith([.. RangeParameters(range), GroupingParameter(grouping)]))
                .ToListAsync(cancellationToken);

            return [.. rows.Select(row => new UsageEventBucketCount(row.BucketStart, UsageEventCountOf(row.Type, row.Detail, row.Count)))];
        }

        public async Task<ActivePeopleCount> CountActivePeopleAsync(StatsDateRange range, CancellationToken cancellationToken)
        {
            List<ActivePeopleRow> rows = await _context.Database
                .SqlQueryRaw<ActivePeopleRow>(ActivePeopleSql, ParametersWith([.. RangeParameters(range), ActiveParameter()]))
                .ToListAsync(cancellationToken);
            ActivePeopleRow row = rows.Single();

            return new ActivePeopleCount(row.People, row.PersonDays, row.Guests);
        }

        public async Task<IReadOnlyList<ActivePeopleBucketCount>> CountActivePeoplePerBucketAsync(StatsDateRange range, StatsGrouping grouping, CancellationToken cancellationToken)
        {
            List<ActivePeopleBucketRow> rows = await _context.Database
                .SqlQueryRaw<ActivePeopleBucketRow>(ActivePeoplePerBucketSql, ParametersWith([.. RangeParameters(range), ActiveParameter(), GroupingParameter(grouping)]))
                .ToListAsync(cancellationToken);

            return [.. rows.Select(row => new ActivePeopleBucketCount(row.BucketStart, row.People, row.PersonDays))];
        }

        private static UsageEventCount UsageEventCountOf(string type, string? detail, int count)
        {
            return new UsageEventCount(Enum.Parse<UsageEventType>(type), detail, count);
        }

        private static object[] ParametersWith(params NpgsqlParameter[] queryParameters)
        {
            return [
                new NpgsqlParameter("time_zone", StatsCalendar.TimeZoneId),
                new NpgsqlParameter("admin_role", Roles.Admin),
                .. queryParameters];
        }

        private static NpgsqlParameter[] RangeParameters(StatsDateRange range)
        {
            return [new NpgsqlParameter("from", range.From), new NpgsqlParameter("to", range.To)];
        }

        private static NpgsqlParameter GroupingParameter(StatsGrouping grouping)
        {
            return new NpgsqlParameter("grouping", StatsCalendar.NameOf(grouping));
        }

        private static NpgsqlParameter ActiveParameter()
        {
            return new NpgsqlParameter("active", UsageEventType.Active.ToString());
        }
    }
}
