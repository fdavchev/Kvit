using System.Text.Json;
using Kvit.Api.Tests.Groups;

namespace Kvit.Api.Tests.AccountDeletion
{
    public static class DeletionRequests
    {
        public const string DeletePath = "/api/me/delete";
        public const string KeepName = "KeepName";
        public const string FormerMember = "FormerMember";
        public const string NameChoiceInvalidCode = "ACCOUNT_DELETE_NAME_CHOICE_INVALID";
        public const string FormerMemberEnglish = "Former member";
        public const string FormerMemberMacedonian = "Поранешен член";

        public static readonly TimeSpan GraceTime = TimeSpan.FromDays(30);

        public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, string nameChoice)
        {
            return DeleteRawAsync(client, JsonSerializer.Serialize(new { nameChoice }));
        }

        public static Task<HttpResponseMessage> DeleteRawAsync(HttpClient client, string json)
        {
            return GroupRequests.PostJsonTextAsync(client, DeletePath, json);
        }
    }
}
