namespace Kvit.Domain.UsageEvents
{
    public sealed class ActiveDayMemory
    {
        private readonly Lock _lock = new();
        private readonly HashSet<Guid> _countedUserIds = [];
        private DateOnly _day = DateOnly.MinValue;

        public bool IsCounted(Guid userId, DateOnly day)
        {
            lock (_lock)
            {
                return day == _day && _countedUserIds.Contains(userId);
            }
        }

        public void Remember(Guid userId, DateOnly day)
        {
            lock (_lock)
            {
                if (day < _day)
                {
                    return;
                }

                if (day > _day)
                {
                    _day = day;
                    _countedUserIds.Clear();
                }

                _countedUserIds.Add(userId);
            }
        }
    }
}
