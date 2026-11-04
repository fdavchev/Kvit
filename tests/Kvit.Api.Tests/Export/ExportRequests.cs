using System.Net.Http.Headers;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public static class ExportRequests
    {
        public const string Path = "/api/me/export";
        public const string PrivacyNoticeUrl = "https://kvit-mk.pages.dev/privacy";
        public const string FileNamePrefix = "kvit-my-data-";
        public const string FileNameSuffix = ".json";

        public static readonly string[] TopLevelFieldNames = Sorted("exportedAt", "groups", "personalBudget", "privacyNotice", "profile");

        public static readonly string[] ProfileFieldNames = Sorted(
            "budgetTotalCurrency",
            "createdAt",
            "displayName",
            "email",
            "googleLinked",
            "hasPassword",
            "id",
            "isGuest",
            "language",
            "termsAcceptedAt",
            "termsVersion",
            "timeZone");

        public static readonly string[] PersonalBudgetFieldNames = Sorted("budgetLimits", "categories", "duplicateAnswers", "entries", "recurringIncomes");

        public static readonly string[] CategoryFieldNames = Sorted("archivedAt", "color", "emoji", "id", "name", "sortOrder");

        public static readonly string[] EntryFieldNames = Sorted(
            "amountMinor",
            "category",
            "categoryId",
            "createdAt",
            "currency",
            "date",
            "id",
            "kind",
            "mkdPerEur",
            "note",
            "rateDate",
            "recurringIncomeId",
            "updatedAt");

        public static readonly string[] RecurringIncomeFieldNames = Sorted(
            "amountMinor",
            "createdAt",
            "currency",
            "dayOfMonth",
            "dayOfWeek",
            "endsOn",
            "frequency",
            "id",
            "startsOn",
            "stoppedAt",
            "title");

        public static readonly string[] BudgetLimitFieldNames = Sorted("categoryId", "createdAt", "effectiveFrom", "limitMinor", "updatedAt");

        public static readonly string[] DuplicateAnswerFieldNames = Sorted("answer", "createdAt", "expenseId", "personalEntryId");

        public static readonly string[] GroupFieldNames = Sorted(
            "countsInBudget",
            "defaultCurrency",
            "emoji",
            "expenses",
            "id",
            "isOwner",
            "members",
            "membership",
            "name",
            "payments",
            "status");

        public static readonly string[] MembershipFieldNames = Sorted("endKind", "joinedAt", "memberId", "removedAt");

        public static readonly string[] MemberFieldNames = Sorted("displayName", "endKind", "isPlainName", "isYou", "joinedAt", "memberId", "removedAt");

        public static readonly string[] ExpenseFieldNames = Sorted(
            "amountMinor",
            "category",
            "createdAt",
            "currency",
            "date",
            "id",
            "note",
            "paidByMemberId",
            "shares",
            "splitType",
            "title");

        public static readonly string[] ShareFieldNames = Sorted("amountMinor", "memberId");

        public static readonly string[] PaymentFieldNames = Sorted(
            "amountMinor",
            "createdAt",
            "currency",
            "fromMemberId",
            "id",
            "resolvedAt",
            "status",
            "toMemberId");

        public static string[] Sorted(params string[] names)
        {
            return [.. names.Order(StringComparer.Ordinal)];
        }

        public static Task<HttpResponseMessage> GetAsync(HttpClient client)
        {
            return client.GetAsync(Path, TestContext.Current.CancellationToken);
        }

        public static async Task<ExportBody> GetBodyAsync(HttpClient client)
        {
            return await ExportBody.ReadAsync(await GetAsync(client));
        }

        public static string FileNameOn(DateOnly day)
        {
            return $"{FileNamePrefix}{day:yyyy-MM-dd}{FileNameSuffix}";
        }

        public static string FileNameOf(HttpResponseMessage response)
        {
            ContentDispositionHeaderValue? disposition = response.Content.Headers.ContentDisposition;

            Assert.NotNull(disposition);
            Assert.Equal("attachment", disposition.DispositionType);
            Assert.NotNull(disposition.FileName);
            return disposition.FileName.Trim('"');
        }
    }
}
