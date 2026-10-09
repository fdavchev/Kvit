using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Api.Tests.Expenses
{
    public sealed class ScriptedExchangeRateSource : IExchangeRateSource
    {
        public static readonly DateOnly DefaultRateDate = new(2026, 10, 5);
        public const decimal DefaultRate = 61.7000m;
        public const string FailureCode = "SCRIPTED_SOURCE_FAILED";

        private FetchedExchangeRate _answer = new(DefaultRateDate, DefaultRate);
        private bool _isFailing;

        public List<DateOnly> RequestedDates { get; } = [];

        public void AnswerWith(DateOnly rateDate, decimal mkdPerEur)
        {
            _answer = new FetchedExchangeRate(rateDate, mkdPerEur);
            _isFailing = false;
        }

        public void Fail()
        {
            _isFailing = true;
        }

        public void Reset()
        {
            _answer = new FetchedExchangeRate(DefaultRateDate, DefaultRate);
            _isFailing = false;
            RequestedDates.Clear();
        }

        public Task<Result<FetchedExchangeRate>> FetchEurRateAsync(DateOnly date, CancellationToken cancellationToken)
        {
            RequestedDates.Add(date);
            if (_isFailing)
            {
                return Task.FromResult(Result.Failure<FetchedExchangeRate>("The scripted exchange rate source was told to fail.", FailureCode));
            }

            return Task.FromResult(Result.Ok(_answer));
        }
    }
}
