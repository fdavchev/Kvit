using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.BudgetCurrency
{
    public class SetBudgetCurrencyTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData("MKD")]
        [InlineData("EUR")]
        public async Task Set_ACurrency_Answers204WithNoBodyAndStoresIt(string currency)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await BudgetCurrencyRequests.SetAsync(user.Client, currency);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(currency, await BudgetCurrencySteps.StoredAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Set_NewAccount_StartsWithNoCurrencyStored()
        {
            SignedInUser user = await _app.SignUpAsync();

            Assert.Null(await BudgetCurrencySteps.StoredAsync(_app, user.UserId));
        }

        [Theory]
        [InlineData("MKD")]
        [InlineData("EUR")]
        public async Task Set_Null_ClearsTheStoredCurrency(string before)
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.SetAsync(user, before);

            HttpResponseMessage response = await BudgetCurrencyRequests.SetAsync(user.Client, null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Null(await BudgetCurrencySteps.StoredAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Set_MkdThenEur_ReplacesTheStoredCurrency()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Mkd);

            HttpResponseMessage response = await BudgetCurrencyRequests.SetAsync(user.Client, BudgetCurrencyRequests.Eur);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(BudgetCurrencyRequests.Eur, await BudgetCurrencySteps.StoredAsync(_app, user.UserId));
        }

        [Theory]
        [InlineData("MKD")]
        [InlineData("EUR")]
        [InlineData(null)]
        public async Task Set_TheSameValueTwice_Answers204BothTimesAndKeepsTheValue(string? currency)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage first = await BudgetCurrencyRequests.SetAsync(user.Client, currency);
            HttpResponseMessage second = await BudgetCurrencyRequests.SetAsync(user.Client, currency);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
            Assert.Equal(currency, await BudgetCurrencySteps.StoredAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Set_ByOnePerson_LeavesEveryOtherPersonAndTheirOwnLanguageAlone()
        {
            SignedInUser changer = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            await BudgetCurrencySteps.SetAsync(other, BudgetCurrencyRequests.Eur);

            await BudgetCurrencySteps.SetAsync(changer, BudgetCurrencyRequests.Mkd);

            Assert.Equal(BudgetCurrencyRequests.Mkd, await BudgetCurrencySteps.StoredAsync(_app, changer.UserId));
            Assert.Equal(BudgetCurrencyRequests.Eur, await BudgetCurrencySteps.StoredAsync(_app, other.UserId));
            JsonElement me = await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(changer.Client));
            Assert.Equal(changer.Form.Language, me.GetProperty("language").GetString());
        }

        [Fact]
        public async Task Set_AnyValue_WritesNoActivityOrUsageEvent()
        {
            SignedInUser user = await _app.SignUpAsync();
            int before = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);

            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Mkd);
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Eur);
            await BudgetCurrencySteps.SetAsync(user, null);

            Assert.Equal(before, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Set_ByAGuest_Answers403GuestNotAllowedAndStoresNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, setup.GroupId, "Gost");

            HttpResponseMessage response = await BudgetCurrencyRequests.SetAsync(guest.Client, BudgetCurrencyRequests.Mkd);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.Null(await BudgetCurrencySteps.StoredAsync(_app, guest.UserId));
        }

        [Fact]
        public async Task Set_Anonymous_Answers401()
        {
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await BudgetCurrencyRequests.SetAsync(anonymous, BudgetCurrencyRequests.Mkd);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("mkd")]
        [InlineData("eur")]
        [InlineData("Eur")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("MKD ")]
        public async Task Set_AValueThatIsNotMkdOrEur_Answers400WithTheBudgetCurrencyCodeAndKeepsTheStoredValue(string currency)
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Eur);

            HttpResponseMessage response = await BudgetCurrencyRequests.SetAsync(user.Client, currency);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.ME_BUDGET_CURRENCY_INVALID);
            Assert.Equal(BudgetCurrencyRequests.Eur, await BudgetCurrencySteps.StoredAsync(_app, user.UserId));
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"budgetTotalCurrency\":1}")]
        [InlineData("{\"budgetTotalCurrency\":true}")]
        [InlineData("{\"budgetTotalCurrency\":[]}")]
        [InlineData("{\"currency\":\"MKD\"}")]
        [InlineData("\"MKD\"")]
        [InlineData("null")]
        [InlineData("not json")]
        [InlineData("")]
        public async Task Set_BodyWithoutTheProperty_Answers400AndKeepsTheStoredValue(string json)
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Eur);

            HttpResponseMessage response = await BudgetCurrencyRequests.SetWithJsonTextAsync(user.Client, json);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(BudgetCurrencyRequests.Eur, await BudgetCurrencySteps.StoredAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Me_AccountThatNeverSetIt_AnswersNullBudgetTotalCurrency()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement me = await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(user.Client));

            Assert.Equal(JsonValueKind.Null, me.GetProperty("budgetTotalCurrency").ValueKind);
        }

        [Theory]
        [InlineData("MKD")]
        [InlineData("EUR")]
        public async Task Me_AfterSetting_AnswersTheNewCurrency(string currency)
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.SetAsync(user, currency);

            JsonElement me = await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(user.Client));

            Assert.Equal(currency, me.GetProperty("budgetTotalCurrency").GetString());
        }

        [Fact]
        public async Task Me_AfterClearing_AnswersNullAgain()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Mkd);
            await BudgetCurrencySteps.SetAsync(user, null);

            JsonElement me = await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(user.Client));

            Assert.Equal(JsonValueKind.Null, me.GetProperty("budgetTotalCurrency").ValueKind);
        }

        [Fact]
        public async Task SignUp_Answer_HasNullBudgetTotalCurrency()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("budgetTotalCurrency").ValueKind);
        }

        [Fact]
        public async Task LogIn_Answer_HasTheStoredBudgetTotalCurrency()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Eur);
            HttpClient otherPhone = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(otherPhone, user.Form.Email, user.Form.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(BudgetCurrencyRequests.Eur, body.GetProperty("budgetTotalCurrency").GetString());
        }
    }
}
