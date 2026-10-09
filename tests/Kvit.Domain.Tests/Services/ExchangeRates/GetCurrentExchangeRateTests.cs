using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Results;
using Kvit.Domain.Services.ExchangeRates;
using Kvit.Domain.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kvit.Domain.Tests.Services.ExchangeRates
{
    public class GetCurrentExchangeRateTests
    {
        private const string FetchFailureMessage = "NBRM answered 500 for 05.10.2026";

        private readonly FakeExchangeRateRepository _repository = new();
        private readonly FakeExchangeRateSource _source = new();
        private readonly AdjustableTimeProvider _time = new(ExchangeRateTestData.Now);
        private readonly RecordingLogger<GetCurrentExchangeRate> _logger = new();
        private readonly GetCurrentExchangeRate _getCurrentExchangeRate;

        public GetCurrentExchangeRateTests()
        {
            ExchangeRateRefreshGate gate = new(_time);
            _getCurrentExchangeRate = new GetCurrentExchangeRate(_repository, _source, gate, _time, _logger);
        }

        [Fact]
        public async Task Execute_NewestRowIs7Hours59MinutesOld_ReturnsItAndNeverCallsTheSource()
        {
            DateTimeOffset fetchedAt = ExchangeRateTestData.Now - TimeSpan.FromHours(7) - TimeSpan.FromMinutes(59);
            _repository.Seed(ExchangeRateTestData.SeededDate, ExchangeRateTestData.SeededRate, fetchedAt);

            Result<ExchangeRateSnapshot> result = await ExecuteAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(new ExchangeRateSnapshot(ExchangeRateTestData.SeededDate, ExchangeRateTestData.SeededRate, fetchedAt), result.Value);
            Assert.Empty(_source.RequestedDates);
            Assert.Empty(_repository.Saved);
            Assert.Empty(_logger.Entries);
        }

        [Theory]
        [InlineData(8 * 60)]
        [InlineData((8 * 60) + 1)]
        [InlineData(3 * 24 * 60)]
        public async Task Execute_NewestRowIsAtLeast8HoursOld_FetchesSavesAndReturnsTheNewSnapshot(int ageInMinutes)
        {
            DateTimeOffset fetchedAt = ExchangeRateTestData.Now - TimeSpan.FromMinutes(ageInMinutes);
            _repository.Seed(ExchangeRateTestData.SeededDate, ExchangeRateTestData.SeededRate, fetchedAt);
            _source.AnswerWith(ExchangeRateTestData.UtcToday, ExchangeRateTestData.FetchedRate);

            Result<ExchangeRateSnapshot> result = await ExecuteAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(new ExchangeRateSnapshot(ExchangeRateTestData.UtcToday, ExchangeRateTestData.FetchedRate, ExchangeRateTestData.Now), result.Value);
            Assert.Equal([ExchangeRateTestData.UtcToday], _source.RequestedDates);
            Assert.Equal(
                [new SavedExchangeRate(ExchangeRateTestData.UtcToday, ExchangeRateTestData.FetchedRate, ExchangeRateTestData.Now)],
                _repository.Saved);
        }

        [Fact]
        public async Task Execute_NewestRowIsOld_AsksTheSourceForTheUtcDateOfNowAndPassesTheCancellationToken()
        {
            SeedOldRow();
            _source.AnswerWith(ExchangeRateTestData.UtcToday, ExchangeRateTestData.FetchedRate);
            using CancellationTokenSource cancellation = new();

            await _getCurrentExchangeRate.Execute(cancellation.Token);

            Assert.Equal([new DateOnly(2026, 10, 5)], _source.RequestedDates);
            Assert.Equal([cancellation.Token], _source.ReceivedTokens);
        }

        [Fact]
        public async Task Execute_SourceAnswersWithAnEarlierRateDate_SavesAndReturnsThatRateDateWithFetchedAtNow()
        {
            SeedOldRow();
            _source.AnswerWith(ExchangeRateTestData.LastFriday, ExchangeRateTestData.FetchedRate);

            Result<ExchangeRateSnapshot> result = await ExecuteAsync();

            Assert.Equal(new ExchangeRateSnapshot(ExchangeRateTestData.LastFriday, ExchangeRateTestData.FetchedRate, ExchangeRateTestData.Now), result.Value);
            Assert.Equal(
                [new SavedExchangeRate(ExchangeRateTestData.LastFriday, ExchangeRateTestData.FetchedRate, ExchangeRateTestData.Now)],
                _repository.Saved);
        }

        [Fact]
        public async Task Execute_SourceAnswersWithTheRateDateAlreadyStored_UpdatesThatRowInsteadOfAddingASecondOne()
        {
            DateTimeOffset fetchedAt = ExchangeRateTestData.Now - TimeSpan.FromHours(30);
            _repository.Seed(ExchangeRateTestData.LastFriday, ExchangeRateTestData.SeededRate, fetchedAt);
            _source.AnswerWith(ExchangeRateTestData.LastFriday, ExchangeRateTestData.SeededRate);

            Result<ExchangeRateSnapshot> result = await ExecuteAsync();

            Assert.Equal(new ExchangeRateSnapshot(ExchangeRateTestData.LastFriday, ExchangeRateTestData.SeededRate, ExchangeRateTestData.Now), result.Value);
            ExchangeRate row = Assert.Single(_repository.Rows);
            Assert.Equal(ExchangeRateTestData.LastFriday, row.RateDate);
            Assert.Equal(ExchangeRateTestData.Now, row.FetchedAt);
        }

        [Fact]
        public async Task Execute_TwoCallsAfterASuccessfulFetch_FetchOnlyOnce()
        {
            SeedOldRow();
            _source.AnswerWith(ExchangeRateTestData.UtcToday, ExchangeRateTestData.FetchedRate);
            await ExecuteAsync();
            _time.Advance(TimeSpan.FromMinutes(5));

            Result<ExchangeRateSnapshot> second = await ExecuteAsync();

            Assert.Single(_source.RequestedDates);
            Assert.Single(_repository.Saved);
            Assert.Equal(ExchangeRateTestData.FetchedRate, second.Value.MkdPerEur);
        }

        [Fact]
        public async Task Execute_FetchFails_ReturnsTheOldRowAsASuccessAndSavesNothing()
        {
            DateTimeOffset fetchedAt = SeedOldRow();
            _source.FailWith(FetchFailureMessage);

            Result<ExchangeRateSnapshot> result = await ExecuteAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(new ExchangeRateSnapshot(ExchangeRateTestData.SeededDate, ExchangeRateTestData.SeededRate, fetchedAt), result.Value);
            Assert.Empty(_repository.Saved);
        }

        [Fact]
        public async Task Execute_FetchFails_LogsAnErrorThatIncludesTheFailureMessage()
        {
            SeedOldRow();
            _source.FailWith(FetchFailureMessage);

            await ExecuteAsync();

            Assert.Contains(_logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains(FetchFailureMessage, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Execute_SecondCallWithin30MinutesOfAFailure_DoesNotFetchAndReturnsTheOldRow()
        {
            DateTimeOffset fetchedAt = SeedOldRow();
            _source.FailWith(FetchFailureMessage);
            await ExecuteAsync();
            _time.Advance(TimeSpan.FromMinutes(29));

            Result<ExchangeRateSnapshot> second = await ExecuteAsync();

            Assert.Single(_source.RequestedDates);
            Assert.True(second.IsSuccess);
            Assert.Equal(new ExchangeRateSnapshot(ExchangeRateTestData.SeededDate, ExchangeRateTestData.SeededRate, fetchedAt), second.Value);
            Assert.Single(_logger.Entries);
        }

        [Fact]
        public async Task Execute_CallMoreThan30MinutesAfterAFailure_FetchesAgainAndReturnsTheNewRate()
        {
            SeedOldRow();
            _source.FailWith(FetchFailureMessage);
            await ExecuteAsync();
            _time.Advance(TimeSpan.FromMinutes(31));
            _source.AnswerWith(ExchangeRateTestData.UtcToday, ExchangeRateTestData.FetchedRate);

            Result<ExchangeRateSnapshot> second = await ExecuteAsync();

            Assert.Equal(2, _source.RequestedDates.Count);
            Assert.Equal(
                new ExchangeRateSnapshot(ExchangeRateTestData.UtcToday, ExchangeRateTestData.FetchedRate, ExchangeRateTestData.Now + TimeSpan.FromMinutes(31)),
                second.Value);
            Assert.Single(_repository.Saved);
        }

        [Fact]
        public async Task Execute_FetchFailsAgainAfterTheWait_StartsAnotherThirtyMinuteWait()
        {
            SeedOldRow();
            _source.FailWith(FetchFailureMessage);
            await ExecuteAsync();
            _time.Advance(TimeSpan.FromMinutes(31));
            await ExecuteAsync();
            _time.Advance(TimeSpan.FromMinutes(29));

            await ExecuteAsync();

            Assert.Equal(2, _source.RequestedDates.Count);
            Assert.Equal(2, _logger.Entries.Count(entry => entry.Level == LogLevel.Error));
        }

        [Fact]
        public async Task Execute_EmptyRepository_ThrowsInvalidOperationExceptionAndNeverCallsTheSource()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteAsync());

            Assert.Empty(_source.RequestedDates);
            Assert.Empty(_repository.Saved);
        }

        private DateTimeOffset SeedOldRow()
        {
            DateTimeOffset fetchedAt = ExchangeRateTestData.Now - TimeSpan.FromHours(30);
            _repository.Seed(ExchangeRateTestData.SeededDate, ExchangeRateTestData.SeededRate, fetchedAt);

            return fetchedAt;
        }

        private Task<Result<ExchangeRateSnapshot>> ExecuteAsync()
        {
            return _getCurrentExchangeRate.Execute(TestContext.Current.CancellationToken);
        }
    }
}
