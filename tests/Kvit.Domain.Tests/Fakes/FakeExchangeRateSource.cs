using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakeExchangeRateSource : IExchangeRateSource
    {
        private Result<FetchedExchangeRate> _answer = Result.Failure<FetchedExchangeRate>("The fake source has no answer set.", "FAKE_SOURCE_NOT_SET_UP");

        private readonly HashSet<DateOnly> _notPublishedDates = [];

        public List<DateOnly> RequestedDates { get; } = [];

        public List<CancellationToken> ReceivedTokens { get; } = [];

        public void AnswerWith(DateOnly rateDate, decimal mkdPerEur)
        {
            _answer = Result.Ok(new FetchedExchangeRate(rateDate, mkdPerEur));
        }

        public void FailWith(string message)
        {
            _answer = Result.Failure<FetchedExchangeRate>(message, "FAKE_SOURCE_FAILED");
        }

        public void HasNothingPublishedOn(params DateOnly[] dates)
        {
            _notPublishedDates.UnionWith(dates);
        }

        public Task<Result<FetchedExchangeRate>> FetchEurRateAsync(DateOnly date, CancellationToken cancellationToken)
        {
            RequestedDates.Add(date);
            ReceivedTokens.Add(cancellationToken);
            if (_notPublishedDates.Contains(date))
            {
                return Task.FromResult(Result.Failure<FetchedExchangeRate>($"The fake source has no EUR entry for {date}.", ExchangeRateFailureCodes.EXCHANGE_RATE_NOT_PUBLISHED));
            }

            return Task.FromResult(_answer);
        }
    }
}
