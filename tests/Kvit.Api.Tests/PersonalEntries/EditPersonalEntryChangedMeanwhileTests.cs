using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class EditPersonalEntryChangedMeanwhileTests(EntryChangedMeanwhileApp _app) : IClassFixture<EntryChangedMeanwhileApp>
    {
        private const decimal CurrentRate = 62.2500m;

        [Fact]
        public async Task Edit_InMkdWhileAnotherRequestSwitchedTheEntryToEurJustBefore_Answers200WithTheCurrentRate()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, CurrentRate, TimeSpan.FromHours(1));
            _app.ChangeAfterNextTransactionOpens.Arm(() => SwitchToEurAsync(entryId));

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(user.Client, entryId, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 150_000, "MKD"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("MKD", body.GetProperty("currency").GetString());
            Assert.Equal(150_000, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal(CurrentRate, body.GetProperty("mkdPerEur").GetDecimal());
        }

        [Fact]
        public async Task Edit_InMkdWhileAnotherRequestSwitchedTheEntryToEurJustBefore_StoresMkdWithTheCurrentRate()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, CurrentRate, TimeSpan.FromHours(1));
            _app.ChangeAfterNextTransactionOpens.Arm(() => SwitchToEurAsync(entryId));

            await PersonalEntryRequests.EditAsync(user.Client, entryId, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 150_000, "MKD"));

            Assert.Equal("MKD", await PersonalEntrySteps.ValueAsync(_app, entryId, "currency"));
            Assert.Equal("62.2500", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
        }

        private Task SwitchToEurAsync(Guid entryId)
        {
            return _app.ExecuteAsync(
                "UPDATE personal_entries SET currency = 'EUR', amount_minor = 1000, mkd_per_eur = 60.0000 WHERE id = @id",
                new NpgsqlParameter("id", entryId));
        }
    }
}
