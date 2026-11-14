using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class DeleteAccountChoiceTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData("{}")]
        [InlineData("{\"nameChoice\":null}")]
        [InlineData("{\"nameChoice\":\"\"}")]
        [InlineData("{\"nameChoice\":\" \"}")]
        [InlineData("{\"nameChoice\":\"Other\"}")]
        [InlineData("{\"nameChoice\":\"keepname\"}")]
        [InlineData("{\"nameChoice\":\"KEEPNAME\"}")]
        [InlineData("{\"nameChoice\":\"formermember\"}")]
        [InlineData("{\"nameChoice\":\"Former member\"}")]
        [InlineData("{\"nameChoice\":\"Keep my name\"}")]
        [InlineData("{\"nameChoice\":\"KeepName \"}")]
        public async Task Delete_MissingOrUnknownChoice_Answers400WithTheNameChoiceInvalidCode(string json)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await DeletionRequests.DeleteRawAsync(user.Client, json);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, DeletionRequests.NameChoiceInvalidCode);
        }

        [Fact]
        public async Task Delete_UnknownChoice_StoresNothingAndKeepsTheSessionAlive()
        {
            SignedInUser user = await _app.SignUpAsync();
            string stampBefore = await DeletionRows.UserValueAsync(_app, user.UserId, "security_stamp") ?? string.Empty;

            await DeletionRequests.DeleteRawAsync(user.Client, "{\"nameChoice\":\"Other\"}");

            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, user.UserId));
            Assert.Equal(stampBefore, await DeletionRows.UserValueAsync(_app, user.UserId, "security_stamp"));
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(user.Client)).StatusCode);
        }

        [Fact]
        public async Task Delete_MissingChoice_StoresNothingAndKeepsTheSessionAlive()
        {
            SignedInUser user = await _app.SignUpAsync();

            await DeletionRequests.DeleteRawAsync(user.Client, "{}");

            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, user.UserId));
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(user.Client)).StatusCode);
        }

        [Fact]
        public async Task Delete_InvalidChoiceThenValidChoice_StoresTheValidChoice()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionRequests.DeleteRawAsync(user.Client, "{\"nameChoice\":\"Other\"}");

            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.FormerMember);

            StoredDeletion stored = await DeletionRows.ReadAsync(_app, user.UserId);
            Assert.Equal(DeletionRequests.FormerMember, stored.NameChoice);
            Assert.NotNull(stored.RequestedAt);
        }
    }
}
