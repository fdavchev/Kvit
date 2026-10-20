using System.Net.Http.Json;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public static class RecurringIncomeRequests
    {
        public const string RecurringIncomesPath = "/api/personal/recurring-incomes";
        public const string CatchUpPath = "/api/personal/recurring-incomes/catch-up";
        public const int ActiveLimit = 20;

        public static readonly string[] RowFieldNames = ["amountMinor", "createdAt", "currency", "dayOfMonth", "dayOfWeek", "endsOn", "frequency", "id", "nextDate", "startsOn", "title"];

        public static TheoryData<string> AllEndpoints => ["list", "create", "edit", "stop", "restart", "catchUp"];

        public static TheoryData<string> EndpointsWithAnId => ["edit", "stop", "restart"];

        public static string PathOf(Guid recurringIncomeId)
        {
            return $"{RecurringIncomesPath}/{recurringIncomeId}";
        }

        public static Task<HttpResponseMessage> ListAsync(HttpClient client)
        {
            return client.GetAsync(RecurringIncomesPath, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> CreateAsync(HttpClient client, RecurringIncomeBody body, Guid? clientRequestId = null)
        {
            return client.PostAsJsonAsync(
                RecurringIncomesPath,
                new
                {
                    clientRequestId = clientRequestId ?? Guid.CreateVersion7(),
                    amountMinor = body.AmountMinor,
                    currency = body.Currency,
                    title = body.Title,
                    frequency = body.Frequency,
                    dayOfMonth = body.DayOfMonth,
                    dayOfWeek = body.DayOfWeek,
                    startsOn = body.StartsOn,
                    endsOn = body.EndsOn,
                },
                TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> EditAsync(HttpClient client, Guid recurringIncomeId, RecurringIncomeBody body)
        {
            return client.PutAsJsonAsync(
                PathOf(recurringIncomeId),
                new
                {
                    amountMinor = body.AmountMinor,
                    currency = body.Currency,
                    title = body.Title,
                    frequency = body.Frequency,
                    dayOfMonth = body.DayOfMonth,
                    dayOfWeek = body.DayOfWeek,
                    startsOn = body.StartsOn,
                    endsOn = body.EndsOn,
                },
                TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> StopAsync(HttpClient client, Guid recurringIncomeId)
        {
            return client.PostAsync($"{PathOf(recurringIncomeId)}/stop", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> RestartAsync(HttpClient client, Guid recurringIncomeId)
        {
            return client.PostAsync($"{PathOf(recurringIncomeId)}/restart", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> CatchUpAsync(HttpClient client)
        {
            return client.PostAsync(CatchUpPath, null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> SendAsync(HttpClient client, string endpoint, Guid recurringIncomeId)
        {
            RecurringIncomeBody body = RecurringIncomeBody.Monthly();

            return endpoint switch
            {
                "list" => ListAsync(client),
                "create" => CreateAsync(client, body),
                "edit" => EditAsync(client, recurringIncomeId, body with { AmountMinor = 5_000_000 }),
                "stop" => StopAsync(client, recurringIncomeId),
                "restart" => RestartAsync(client, recurringIncomeId),
                "catchUp" => CatchUpAsync(client),
                _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Not a repeating income endpoint."),
            };
        }
    }
}
