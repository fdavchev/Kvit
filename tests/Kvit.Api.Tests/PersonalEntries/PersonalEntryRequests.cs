using System.Net.Http.Json;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public static class PersonalEntryRequests
    {
        public const string EntriesPath = "/api/personal/entries";
        public const string Mkd = "MKD";
        public const string Eur = "EUR";
        public const long OneThousandMkd = 100_000;
        public const string DefaultMonth = "2026-10";
        public const string DefaultDate = "2026-10-04";

        public static readonly Guid BuiltInFoodId = Guid.Parse("c47e9a10-5f3b-4d2e-9a61-000000000001");
        public static readonly Guid BuiltInTransportId = Guid.Parse("c47e9a10-5f3b-4d2e-9a61-000000000003");

        public const string IncomeKind = "Income";
        public const string SpendingKind = "Spending";
        public const string IncomeHasCategoryCode = ResultCodes.PERSONAL_ENTRY_INCOME_HAS_CATEGORY;
        public const string IncomeTotalAmountProperty = "totalMinor";
        public const string KindCannotChangeCode = ResultCodes.PERSONAL_ENTRY_KIND_CANNOT_CHANGE;

        public static readonly string[] EntryFieldNames = ["amountMinor", "category", "categoryId", "createdAt", "currency", "entryDate", "id", "kind", "mkdPerEur", "note", "rateDate", "updatedAt"];

        public static TheoryData<string> AllEndpoints => ["list", "add", "edit", "delete", "restore"];

        public static TheoryData<string> EndpointsWithAnId => ["edit", "delete", "restore"];

        public static string PathOf(Guid entryId)
        {
            return $"{EntriesPath}/{entryId}";
        }

        public static Task<HttpResponseMessage> ListAsync(HttpClient client, string month = DefaultMonth)
        {
            return client.GetAsync($"{EntriesPath}?month={Uri.EscapeDataString(month)}", TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ListWithoutMonthAsync(HttpClient client)
        {
            return client.GetAsync(EntriesPath, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> AddAsync(HttpClient client, EntryInput input, Guid? clientRequestId = null)
        {
            return client.PostAsJsonAsync(
                EntriesPath,
                new
                {
                    clientRequestId = clientRequestId ?? Guid.CreateVersion7(),
                    categoryId = input.CategoryId,
                    amountMinor = input.AmountMinor,
                    currency = input.Currency,
                    entryDate = input.EntryDate,
                    note = input.Note,
                },
                TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> EditAsync(HttpClient client, Guid entryId, EntryInput input)
        {
            return client.PutAsJsonAsync(
                PathOf(entryId),
                new
                {
                    categoryId = input.CategoryId,
                    amountMinor = input.AmountMinor,
                    currency = input.Currency,
                    entryDate = input.EntryDate,
                    note = input.Note,
                },
                TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> AddIncomeAsync(HttpClient client, IncomeEntryInput input, Guid? clientRequestId = null)
        {
            return AddWithBodyAsync(
                client,
                new
                {
                    clientRequestId = clientRequestId ?? Guid.CreateVersion7(),
                    kind = IncomeKind,
                    amountMinor = input.AmountMinor,
                    currency = input.Currency,
                    entryDate = input.EntryDate,
                    note = input.Note,
                });
        }

        public static Task<HttpResponseMessage> AddIncomeWithCategoryAsync(HttpClient client, IncomeEntryInput input, Guid categoryId)
        {
            return AddWithBodyAsync(
                client,
                new
                {
                    clientRequestId = Guid.CreateVersion7(),
                    kind = IncomeKind,
                    categoryId,
                    amountMinor = input.AmountMinor,
                    currency = input.Currency,
                    entryDate = input.EntryDate,
                    note = input.Note,
                });
        }

        public static Task<HttpResponseMessage> EditIncomeAsync(HttpClient client, Guid entryId, IncomeEntryInput input)
        {
            return EditWithBodyAsync(
                client,
                entryId,
                new
                {
                    kind = IncomeKind,
                    amountMinor = input.AmountMinor,
                    currency = input.Currency,
                    entryDate = input.EntryDate,
                    note = input.Note,
                });
        }

        public static Task<HttpResponseMessage> AddWithBodyAsync(HttpClient client, object body)
        {
            return client.PostAsJsonAsync(EntriesPath, body, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> EditWithBodyAsync(HttpClient client, Guid entryId, object body)
        {
            return client.PutAsJsonAsync(PathOf(entryId), body, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, Guid entryId)
        {
            return client.DeleteAsync(PathOf(entryId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> RestoreAsync(HttpClient client, Guid entryId)
        {
            return client.PostAsync($"{PathOf(entryId)}/restore", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> SendAsync(HttpClient client, string endpoint, Guid entryId)
        {
            EntryInput input = EntryInput.Of(BuiltInFoodId);

            return endpoint switch
            {
                "list" => ListAsync(client),
                "add" => AddAsync(client, input),
                "edit" => EditAsync(client, entryId, input with { AmountMinor = 250_000 }),
                "delete" => DeleteAsync(client, entryId),
                "restore" => RestoreAsync(client, entryId),
                _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Not a personal entry endpoint."),
            };
        }
    }
}
