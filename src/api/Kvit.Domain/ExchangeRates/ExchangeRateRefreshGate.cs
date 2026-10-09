namespace Kvit.Domain.ExchangeRates
{
    public sealed class ExchangeRateRefreshGate(TimeProvider _timeProvider)
    {
        public const int ExchangeRateRetryAfterFailureMinutes = 30;

        private readonly Lock _lock = new();
        private DateTimeOffset? _nextTryAllowedAt;

        public bool CanTry()
        {
            lock (_lock)
            {
                return _nextTryAllowedAt is not DateTimeOffset nextTryAllowedAt || _timeProvider.GetUtcNow() >= nextTryAllowedAt;
            }
        }

        public void RecordFailure()
        {
            lock (_lock)
            {
                _nextTryAllowedAt = _timeProvider.GetUtcNow().AddMinutes(ExchangeRateRetryAfterFailureMinutes);
            }
        }
    }
}
