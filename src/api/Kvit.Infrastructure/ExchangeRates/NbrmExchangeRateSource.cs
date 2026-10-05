using System.Globalization;
using System.Text.Json;
using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Infrastructure.ExchangeRates
{
    public sealed class NbrmExchangeRateSource(HttpClient _httpClient) : IExchangeRateSource
    {
        private const string NbrmDateFormat = "dd.MM.yyyy";
        private const string EuroCode = "EUR";

        public async Task<Result<FetchedExchangeRate>> FetchEurRateAsync(DateOnly date, CancellationToken cancellationToken)
        {
            string nbrmDate = date.ToString(NbrmDateFormat, CultureInfo.InvariantCulture);
            string path = $"GetExchangeRate?StartDate={nbrmDate}&EndDate={nbrmDate}&format=json";

            string body;
            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync(path, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return Failed(date, $"NBRM answered {(int)response.StatusCode} {response.StatusCode}");
                }

                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                return Failed(date, $"the request failed: {exception.Message}");
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                return Failed(date, $"the request timed out: {exception.Message}");
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(body);
                return EuroRateOf(document.RootElement, date);
            }
            catch (JsonException exception)
            {
                return Failed(date, $"the answer is not valid JSON: {exception.Message}");
            }
        }

        private static Result<FetchedExchangeRate> EuroRateOf(JsonElement answer, DateOnly date)
        {
            if (answer.ValueKind != JsonValueKind.Array)
            {
                return Failed(date, $"the answer is a JSON {answer.ValueKind}, not a list of currencies");
            }

            foreach (JsonElement entry in answer.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    return Failed(date, $"the list holds a JSON {entry.ValueKind} instead of a currency");
                }

                if (entry.TryGetProperty("oznaka", out JsonElement code) && code.ValueKind == JsonValueKind.String && code.GetString() == EuroCode)
                {
                    return RateOf(entry, date);
                }
            }

            return Failed(date, $"the answer has no {EuroCode} entry");
        }

        private static Result<FetchedExchangeRate> RateOf(JsonElement euro, DateOnly date)
        {
            if (!TryGetDecimal(euro, "sreden", out decimal middleRate))
            {
                return Failed(date, $"the {EuroCode} entry has no number 'sreden'");
            }

            if (!TryGetDecimal(euro, "nomin", out decimal nominal) || nominal <= 0)
            {
                return Failed(date, $"the {EuroCode} entry has no 'nomin' above 0");
            }

            if (!euro.TryGetProperty("datum", out JsonElement datum) || datum.ValueKind != JsonValueKind.String || !datum.TryGetDateTimeOffset(out DateTimeOffset rateMoment))
            {
                return Failed(date, $"the {EuroCode} entry has no date 'datum'");
            }

            decimal mkdPerEur = decimal.Round(middleRate / nominal, ExchangeRate.MkdPerEurDecimals, MidpointRounding.AwayFromZero);
            Result rate = ExchangeRate.CheckMkdPerEur(mkdPerEur);
            if (!rate.IsSuccess)
            {
                return Failed(date, rate.Error);
            }

            return Result.Ok(new FetchedExchangeRate(DateOnly.FromDateTime(rateMoment.DateTime), mkdPerEur));
        }

        private static bool TryGetDecimal(JsonElement entry, string propertyName, out decimal value)
        {
            value = 0;
            return entry.TryGetProperty(propertyName, out JsonElement property)
                && property.ValueKind == JsonValueKind.Number
                && property.TryGetDecimal(out value);
        }

        private static Result<FetchedExchangeRate> Failed(DateOnly date, string reason)
        {
            return Result.Failure<FetchedExchangeRate>(
                $"The NBRM euro rate for {date.ToString(NbrmDateFormat, CultureInfo.InvariantCulture)} could not be read: {reason}.",
                ExchangeRateFailureCodes.EXCHANGE_RATE_FETCH_FAILED);
        }
    }
}
