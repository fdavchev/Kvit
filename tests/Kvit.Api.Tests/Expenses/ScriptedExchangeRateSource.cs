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

        public Func<Task>? WhileAnswering { get; set; }

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
            WhileAnswering = null;
            RequestedDates.Clear();
        }

        public async Task<Result<FetchedExchangeRate>> FetchEurRateAsync(DateOnly date, CancellationToken cancellationToken)
        {
            RequestedDates.Add(date);
            if (WhileAnswering is not null)
            {
                await WhileAnswering();
            }

            if (_isFailing)
            {
                return Result.Failure<FetchedExchangeRate>("The scripted exchange rate source was told to fail.", FailureCode);
            }

            return Result.Ok(_answer);
        }
    }
}
