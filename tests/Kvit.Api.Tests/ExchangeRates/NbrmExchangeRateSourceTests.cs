using System.Globalization;
using System.Net;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;
using Kvit.Infrastructure.ExchangeRates;
using Xunit;

namespace Kvit.Api.Tests.ExchangeRates
{
    public class NbrmExchangeRateSourceTests
    {
        private const string BaseUrl = "https://nbrm.test/KLServiceNOV/";
        private static readonly DateOnly SomeDay = new(2026, 9, 24);

        [Fact]
        public async Task FetchEurRateAsync_AnyDate_SendsExactlyOneGetToTheNbrmAddress()
        {
            StubHttpMessageHandler handler = StubHttpMessageHandler.Answering(HttpStatusCode.OK, NbrmAnswers.List(NbrmAnswers.RawEurExample));
            NbrmExchangeRateSource source = CreateSource(handler);

            await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            StubRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                "https://nbrm.test/KLServiceNOV/GetExchangeRate?StartDate=24.09.2026&EndDate=24.09.2026&format=json",
                request.Uri?.AbsoluteUri);
        }

        [Theory]
        [InlineData(2026, 10, 5, "05.10.2026")]
        [InlineData(2026, 1, 9, "09.01.2026")]
        [InlineData(2026, 12, 31, "31.12.2026")]
        public async Task FetchEurRateAsync_Date_IsWrittenAsDayMonthYearWithLeadingZerosInBothParameters(int year, int month, int day, string expectedDate)
        {
            StubHttpMessageHandler handler = StubHttpMessageHandler.Answering(HttpStatusCode.OK, NbrmAnswers.List(NbrmAnswers.RawEurExample));
            NbrmExchangeRateSource source = CreateSource(handler);

            await source.FetchEurRateAsync(new DateOnly(year, month, day), TestContext.Current.CancellationToken);

            StubRequest request = Assert.Single(handler.Requests);
            Assert.Equal($"{BaseUrl}GetExchangeRate?StartDate={expectedDate}&EndDate={expectedDate}&format=json", request.Uri?.AbsoluteUri);
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("ar-SA")]
        [InlineData("mk-MK")]
        public async Task FetchEurRateAsync_UnderAnyCulture_WritesTheDateAndReadsTheRateTheSameWay(string cultureName)
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            try
            {
                StubHttpMessageHandler handler = StubHttpMessageHandler.Answering(HttpStatusCode.OK, NbrmAnswers.List(NbrmAnswers.RawEurExample));
                NbrmExchangeRateSource source = CreateSource(handler);

                Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

                Assert.Equal($"{BaseUrl}GetExchangeRate?StartDate=24.09.2026&EndDate=24.09.2026&format=json", Assert.Single(handler.Requests).Uri?.AbsoluteUri);
                Assert.Equal(new FetchedExchangeRate(SomeDay, 61.561m), result.Value);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Fact]
        public async Task FetchEurRateAsync_RawEurExampleAmongOtherCurrencies_ReturnsTheMiddleRateAndTheDatum()
        {
            string answer = NbrmAnswers.List(NbrmAnswers.Entry("USD", "1", "52.8000"), NbrmAnswers.RawEurExample, NbrmAnswers.Entry("GBP", "1", "70.2500"));
            NbrmExchangeRateSource source = CreateSource(StubHttpMessageHandler.Answering(HttpStatusCode.OK, answer));

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(61.561m, result.Value.MkdPerEur);
            Assert.Equal(new DateOnly(2026, 9, 24), result.Value.RateDate);
        }

        [Fact]
        public async Task FetchEurRateAsync_EurIsTheFirstAndOnlyEntry_ReturnsItsRate()
        {
            NbrmExchangeRateSource source = CreateSource(StubHttpMessageHandler.Answering(HttpStatusCode.OK, NbrmAnswers.List(NbrmAnswers.RawEurExample)));

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            Assert.Equal(new FetchedExchangeRate(new DateOnly(2026, 9, 24), 61.561m), result.Value);
        }

        [Fact]
        public async Task FetchEurRateAsync_RateWithFourDecimals_KeepsEveryDigit()
        {
            string answer = NbrmAnswers.ListWithOtherCurrencies(NbrmAnswers.Eur("1", "61.6333"));
            NbrmExchangeRateSource source = CreateSource(StubHttpMessageHandler.Answering(HttpStatusCode.OK, answer));

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            Assert.Equal(61.6333m, result.Value.MkdPerEur);
        }

        [Fact]
        public async Task FetchEurRateAsync_RateDateComesFromTheAnswerAndNotFromTheRequestedDate()
        {
            DateOnly sunday = new(2026, 10, 4);
            string answer = NbrmAnswers.List(NbrmAnswers.Eur("1", "61.5610", "2026-10-02T00:00:00"));
            NbrmExchangeRateSource source = CreateSource(StubHttpMessageHandler.Answering(HttpStatusCode.OK, answer));

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(sunday, TestContext.Current.CancellationToken);

            Assert.Equal(new DateOnly(2026, 10, 2), result.Value.RateDate);
        }

        [Fact]
        public async Task FetchEurRateAsync_NominOf2_DividesTheMiddleRateByTwo()
        {
            string answer = NbrmAnswers.ListWithOtherCurrencies(NbrmAnswers.Eur("2", "123.2666"));
            NbrmExchangeRateSource source = CreateSource(StubHttpMessageHandler.Answering(HttpStatusCode.OK, answer));

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            Assert.Equal(61.6333m, result.Value.MkdPerEur);
        }

        [Fact]
        public async Task FetchEurRateAsync_NominOfZero_FailsWithAMessageAndDoesNotThrow()
        {
            string answer = NbrmAnswers.ListWithOtherCurrencies(NbrmAnswers.Eur("0", "61.5610"));
            NbrmExchangeRateSource source = CreateSource(StubHttpMessageHandler.Answering(HttpStatusCode.OK, answer));

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            AssertFailedWithAMessage(result);
        }

        [Fact]
        public async Task FetchEurRateAsync_NoEurEntry_FailsWithAMessage()
        {
            string answer = NbrmAnswers.List(NbrmAnswers.Entry("USD", "1", "52.8000"), NbrmAnswers.Entry("GBP", "1", "70.2500"));
            NbrmExchangeRateSource source = CreateSource(StubHttpMessageHandler.Answering(HttpStatusCode.OK, answer));

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            AssertFailedWithAMessage(result);
        }

        [Fact]
        public async Task FetchEurRateAsync_EmptyArray_FailsWithAMessage()
        {
            NbrmExchangeRateSource source = CreateSource(StubHttpMessageHandler.Answering(HttpStatusCode.OK, "[]"));

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            AssertFailedWithAMessage(result);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.BadGateway)]
        public async Task FetchEurRateAsync_NonSuccessStatusEvenWithAValidBody_FailsWithAMessage(HttpStatusCode statusCode)
        {
            NbrmExchangeRateSource source = CreateSource(StubHttpMessageHandler.Answering(statusCode, NbrmAnswers.List(NbrmAnswers.RawEurExample)));

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            AssertFailedWithAMessage(result);
        }

        [Theory]
        [InlineData("<html><body>Service unavailable</body></html>")]
        [InlineData("not json at all")]
        [InlineData("")]
        [InlineData("null")]
        [InlineData("{}")]
        [InlineData("""{"oznaka":"EUR","nomin":1,"sreden":61.561}""")]
        [InlineData("""[{"rBr":178,"datum":"2026-09-24T00:00:00","oznaka":"EUR","nomin":1,"sreden":61.561""")]
        public async Task FetchEurRateAsync_BodyThatIsNotAnArrayOfCurrencies_FailsWithAMessage(string body)
        {
            NbrmExchangeRateSource source = CreateSource(StubHttpMessageHandler.Answering(HttpStatusCode.OK, body));

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            AssertFailedWithAMessage(result);
        }

        [Theory]
        [InlineData("""[{"rBr":178,"datum":"2026-09-24T00:00:00","valuta":"978","oznaka":"EUR","nomin":1}]""")]
        [InlineData("""[{"rBr":178,"valuta":"978","oznaka":"EUR","nomin":1,"sreden":61.561}]""")]
        [InlineData("""[{"rBr":178,"datum":"2026-09-24T00:00:00","valuta":"978","oznaka":"EUR","sreden":61.561}]""")]
        [InlineData("""[{"rBr":178,"datum":"yesterday","valuta":"978","oznaka":"EUR","nomin":1,"sreden":61.561}]""")]
        public async Task FetchEurRateAsync_EurEntryWithAMissingOrBrokenField_FailsWithAMessage(string body)
        {
            NbrmExchangeRateSource source = CreateSource(StubHttpMessageHandler.Answering(HttpStatusCode.OK, body));

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            AssertFailedWithAMessage(result);
        }

        [Fact]
        public async Task FetchEurRateAsync_NetworkError_FailsWithAMessageInsteadOfThrowing()
        {
            StubHttpMessageHandler handler = StubHttpMessageHandler.Throwing(new HttpRequestException("The connection was refused."));
            NbrmExchangeRateSource source = CreateSource(handler);

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            AssertFailedWithAMessage(result);
        }

        [Fact]
        public async Task FetchEurRateAsync_TimeoutOfTheHttpClient_FailsWithAMessageInsteadOfThrowing()
        {
            StubHttpMessageHandler handler = StubHttpMessageHandler.Throwing(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 10 seconds elapsing."));
            NbrmExchangeRateSource source = CreateSource(handler);

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(SomeDay, TestContext.Current.CancellationToken);

            AssertFailedWithAMessage(result);
        }

        private static NbrmExchangeRateSource CreateSource(StubHttpMessageHandler handler)
        {
            HttpClient client = new(handler) { BaseAddress = new Uri(BaseUrl) };

            return new NbrmExchangeRateSource(client);
        }

        private static void AssertFailedWithAMessage(Result<FetchedExchangeRate> result)
        {
            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }
    }
}
