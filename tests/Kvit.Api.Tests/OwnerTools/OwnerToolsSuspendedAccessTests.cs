using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Api.Tests.Terms;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsSuspendedAccessTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        private const string UnprotectedPath = "/api/test/unprotected";
        private const int MinimumEndpointsInTheSweep = 60;

        [Theory]
        [MemberData(nameof(SuspendedCalls.Names), MemberType = typeof(SuspendedCalls))]
        public async Task Call_ByASuspendedAdmin_Answers403AccountSuspended(string name)
        {
            SuspendedCall call = SuspendedCalls.Named(name);
            SignedInUser admin = await _app.SignInAdminAsync();
            await OwnerToolsRows.SetSuspendedAtAsync(_app, admin.UserId, _app.Clock.GetUtcNow());
            _app.Clock.Advance(TimeSpan.FromSeconds(1));

            HttpResponseMessage response = await call.SendAsync(admin.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.AccountSuspendedCode);
        }

        [Theory]
        [MemberData(nameof(SuspendedCalls.NamesForAnOrdinaryPerson), MemberType = typeof(SuspendedCalls))]
        public async Task Call_ByASuspendedOrdinaryPerson_Answers403AccountSuspended(string name)
        {
            SuspendedCall call = SuspendedCalls.Named(name);
            SignedInUser person = await _app.SignUpSuspendedAsync();

            HttpResponseMessage response = await call.SendAsync(person.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.AccountSuspendedCode);
        }

        [Fact]
        public async Task EveryAuthenticatedEndpoint_ExceptGetMe_Answers403AccountSuspendedToASuspendedAdmin()
        {
            SignedInUser admin = await _app.SignInAdminAsync();
            await OwnerToolsRows.SetSuspendedAtAsync(_app, admin.UserId, _app.Clock.GetUtcNow());
            _app.Clock.Advance(TimeSpan.FromSeconds(1));
            List<(string Method, string Path)> endpoints = AuthenticatedControllerEndpoints();
            List<string> unexpected = [];

            foreach ((string method, string path) in endpoints)
            {
                using HttpRequestMessage request = new(new HttpMethod(method), path);
                if (method is "POST" or "PUT" or "DELETE" or "PATCH")
                {
                    request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                }

                HttpResponseMessage response = await admin.Client.SendAsync(request, TestContext.Current.CancellationToken);
                string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                bool isRefusedAsSuspended = response.StatusCode == HttpStatusCode.Forbidden && body.Contains(OwnerToolsRequests.AccountSuspendedCode, StringComparison.Ordinal);
                bool isGetMe = method == "GET" && path == "/api/me";
                if (isRefusedAsSuspended == isGetMe)
                {
                    unexpected.Add($"{method} {path} -> {(int)response.StatusCode} {body}");
                }
            }

            Assert.True(endpoints.Count >= MinimumEndpointsInTheSweep, $"Only {endpoints.Count} authenticated endpoints were found, so the sweep may not be looking at the real routes.");
            Assert.Empty(unexpected);
        }

        [Fact]
        public async Task Me_SuspendedPerson_Answers200WithIsSuspendedTrueOnTheVeryNextRequest()
        {
            SignedInUser person = await _app.SignUpAsync("Dragan Ilievski");
            Assert.False(IsSuspendedOf(await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(person.Client))));

            await OwnerToolsRows.SetSuspendedAtAsync(_app, person.UserId, _app.Clock.GetUtcNow());

            HttpResponseMessage response = await AuthRequests.GetMeAsync(person.Client);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement me = await AuthRequests.ReadJsonAsync(response);
            Assert.True(IsSuspendedOf(me));
            Assert.Equal(person.UserId, me.GetProperty("id").GetGuid());
            Assert.Equal("Dragan Ilievski", me.GetProperty("displayName").GetString());
            Assert.Equal(person.Form.Email, me.GetProperty("email").GetString());
        }

        [Fact]
        public async Task Me_PersonWhoIsNotSuspended_AnswersIsSuspendedFalse()
        {
            SignedInUser person = await _app.SignUpAsync();

            HttpResponseMessage response = await AuthRequests.GetMeAsync(person.Client);

            Assert.False(IsSuspendedOf(await AuthRequests.ReadJsonAsync(response)));
        }

        [Fact]
        public async Task Me_SuspendedAdmin_StillAnswers200WithIsAdminTrueAndIsSuspendedTrue()
        {
            SignedInUser admin = await _app.SignInAdminAsync();
            await OwnerToolsRows.SetSuspendedAtAsync(_app, admin.UserId, _app.Clock.GetUtcNow());

            JsonElement me = await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(admin.Client));

            Assert.True(me.GetProperty("isAdmin").GetBoolean());
            Assert.True(IsSuspendedOf(me));
        }

        [Fact]
        public async Task Suspend_ThroughTheEndpoint_BlocksTheVeryNextRequestWithoutANewLogIn()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.ListAsync(person.Client)).StatusCode);

            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            _app.Clock.Advance(TimeSpan.FromSeconds(1));

            await ProblemResponse.AssertAsync(await GroupRequests.ListAsync(person.Client), HttpStatusCode.Forbidden, OwnerToolsRequests.AccountSuspendedCode);
        }

        [Fact]
        public async Task Unsuspend_ThroughTheEndpoint_LetsTheVeryNextRequestThroughWithoutANewLogIn()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            _app.Clock.Advance(TimeSpan.FromSeconds(1));
            await ProblemResponse.AssertAsync(await GroupRequests.ListAsync(person.Client), HttpStatusCode.Forbidden, OwnerToolsRequests.AccountSuspendedCode);

            await OwnerToolsSteps.UnsuspendAsync(admin, person.UserId);
            _app.Clock.Advance(TimeSpan.FromSeconds(1));

            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.ListAsync(person.Client)).StatusCode);
            Assert.False(IsSuspendedOf(await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(person.Client))));
        }

        [Fact]
        public async Task Suspend_PersonWhoIsRefused_StaysSignedInSoMeKeepsWorking()
        {
            SignedInUser person = await _app.SignUpSuspendedAsync();
            await GroupRequests.ListAsync(person.Client);
            await GroupRequests.CreateAsync(person.Client);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(person.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task LogOut_SuspendedPerson_Answers204AndEndsTheSession()
        {
            SignedInUser person = await _app.SignUpSuspendedAsync();

            HttpResponseMessage response = await AuthRequests.LogOutAsync(person.Client);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await AuthRequests.GetMeAsync(person.Client)).StatusCode);
        }

        [Fact]
        public async Task LogIn_SuspendedPerson_Answers200WithIsSuspendedTrueAndASession()
        {
            SignedInUser person = await _app.SignUpSuspendedAsync();
            HttpClient fresh = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(fresh, person.Form.Email, person.Form.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(AuthCookie.Find(response));
            Assert.True(IsSuspendedOf(await AuthRequests.ReadJsonAsync(response)));
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(fresh)).StatusCode);
            await ProblemResponse.AssertAsync(await GroupRequests.ListAsync(fresh), HttpStatusCode.Forbidden, OwnerToolsRequests.AccountSuspendedCode);
        }

        [Fact]
        public async Task LogIn_SuspendedPersonWithAWrongPassword_Answers401InvalidCredentialsNotTheSuspension()
        {
            SignedInUser person = await _app.SignUpSuspendedAsync();

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), person.Form.Email, AuthRequests.WrongPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_INVALID_CREDENTIALS);
        }

        [Fact]
        public async Task LogIn_PersonAfterBeingUnsuspended_AnswersIsSuspendedFalse()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            await OwnerToolsSteps.UnsuspendAsync(admin, person.UserId);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), person.Form.Email, person.Form.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(IsSuspendedOf(await AuthRequests.ReadJsonAsync(response)));
        }

        [Fact]
        public async Task LogIn_PersonWhoIsNotSuspended_AnswersIsSuspendedFalse()
        {
            SignedInUser person = await _app.SignUpAsync();

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), person.Form.Email, person.Form.Password);

            Assert.False(IsSuspendedOf(await AuthRequests.ReadJsonAsync(response)));
        }

        [Fact]
        public async Task GoogleLogIn_SuspendedPerson_Answers200WithIsSuspendedTrue()
        {
            GoogleAccount account = await DeletionSteps.SignUpWithGoogleAsync(_app);
            await OwnerToolsRows.SetSuspendedAtAsync(_app, account.UserId, _app.Clock.GetUtcNow());

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(_app.CreateClient(), _app.AddGoogleToken(account.Identity));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(IsSuspendedOf(await AuthRequests.ReadJsonAsync(response)));
        }

        [Fact]
        public async Task GoogleLogIn_PersonWhoIsNotSuspended_AnswersIsSuspendedFalse()
        {
            GoogleAccount account = await DeletionSteps.SignUpWithGoogleAsync(_app);

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(_app.CreateClient(), _app.AddGoogleToken(account.Identity));

            Assert.False(IsSuspendedOf(await AuthRequests.ReadJsonAsync(response)));
        }

        [Fact]
        public async Task EndpointWithoutAnyAuthorizationAttribute_ByASuspendedPerson_Answers403AccountSuspended()
        {
            HttpClient client = _app.CreateClientWithTestControllers();
            RegistrationForm form = RegistrationForm.Valid();
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.RegisterAsync(client, form)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(UnprotectedPath, TestContext.Current.CancellationToken)).StatusCode);
            await OwnerToolsRows.SetSuspendedAtAsync(_app, (await _app.FindUserAsync(form.Email)).Id, _app.Clock.GetUtcNow());
            _app.Clock.Advance(TimeSpan.FromSeconds(1));

            HttpResponseMessage response = await client.GetAsync(UnprotectedPath, TestContext.Current.CancellationToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.AccountSuspendedCode);
        }

        [Theory]
        [InlineData("/health")]
        [InlineData("/api/health")]
        public async Task Health_ByASuspendedPerson_StillAnswers200(string path)
        {
            SignedInUser person = await _app.SignUpSuspendedAsync();

            HttpResponseMessage response = await person.Client.GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData("/health")]
        [InlineData("/api/health")]
        public async Task Health_ByAnonymousVisitor_StillAnswers200(string path)
        {
            HttpResponseMessage response = await _app.CreateClientWithoutCookies().GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task InvitePreview_ByASuspendedPerson_StillAnswers200BecauseItIsAnAnonymousEndpoint()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser person = await _app.SignUpSuspendedAsync();

            HttpResponseMessage response = await GroupRequests.PreviewAsync(person.Client, setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DeleteAccount_ByASuspendedPerson_IsRefusedAndStoresNoDeletion()
        {
            SignedInUser person = await _app.SignUpSuspendedAsync();

            await DeletionRequests.DeleteAsync(person.Client, DeletionRequests.KeepName);

            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, person.UserId));
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(person.Client)).StatusCode);
        }

        [Fact]
        public async Task ChangeLanguage_ByASuspendedPerson_IsRefusedAndKeepsTheLanguage()
        {
            SignedInUser person = await _app.SignUpSuspendedAsync();

            await AuthRequests.ChangeLanguageAsync(person.Client, "mk");

            Assert.Equal(person.Form.Language, (await _app.FindUserAsync(person.Form.Email)).Language);
        }

        [Fact]
        public async Task AcceptTerms_ByASuspendedPerson_IsRefusedAndStoresNoAcceptance()
        {
            SignedInUser person = await _app.SignUpSuspendedAsync();
            await TermsRows.ClearAsync(_app, person.UserId);

            await person.Client.PostAsync("/api/me/terms/accept", null, TestContext.Current.CancellationToken);

            Assert.Equal(StoredTerms.None, await TermsRows.ReadAsync(_app, person.UserId));
        }

        [Fact]
        public async Task CreateGroup_ByASuspendedPerson_IsRefusedAndCreatesNoGroup()
        {
            SignedInUser person = await _app.SignUpSuspendedAsync();

            await GroupRequests.CreateAsync(person.Client);

            Assert.Equal(0, await GroupRows.CountGroupsOwnedByAsync(_app, person.UserId));
        }

        [Fact]
        public async Task Export_ByASuspendedPerson_IsRefusedWithNoFileBody()
        {
            SignedInUser person = await _app.SignUpSuspendedAsync("Dragan Ilievski");

            HttpResponseMessage response = await person.Client.GetAsync("/api/me/export", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.DoesNotContain("Dragan Ilievski", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        }

        [Fact]
        public async Task OtherPeople_ViewingTheGroupOfASuspendedPerson_SeeTheSameMembersAsBefore()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            string membersBefore = await group.Owner.Client.GetStringAsync(GroupRequests.MembersPathOf(group.GroupId), TestContext.Current.CancellationToken);
            string groupBefore = await group.Owner.Client.GetStringAsync(GroupRequests.PathOf(group.GroupId), TestContext.Current.CancellationToken);

            await OwnerToolsRows.SetSuspendedAtAsync(_app, group.Member.UserId, _app.Clock.GetUtcNow());

            Assert.Equal(membersBefore, await group.Owner.Client.GetStringAsync(GroupRequests.MembersPathOf(group.GroupId), TestContext.Current.CancellationToken));
            Assert.Equal(groupBefore, await group.Owner.Client.GetStringAsync(GroupRequests.PathOf(group.GroupId), TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task OtherMembers_OfAGroupWhoseOwnerIsSuspended_StillAddExpensesAsUsual()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await OwnerToolsRows.SetSuspendedAtAsync(_app, group.Owner.UserId, _app.Clock.GetUtcNow());

            HttpResponseMessage response = await ExpenseRequests.AddAsync(
                group.Member.Client,
                group.GroupId,
                ExpenseInputs.Equal(60_000, ExpenseInputs.Mkd, group.MemberRowId, group.MemberRowId, group.OwnerRowId));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static bool IsSuspendedOf(JsonElement me)
        {
            Assert.True(me.TryGetProperty("isSuspended", out JsonElement value), $"The answer has no 'isSuspended' property: {me}");

            return value.GetBoolean();
        }

        private List<(string Method, string Path)> AuthenticatedControllerEndpoints()
        {
            using IServiceScope scope = _app.CreateScope();
            EndpointDataSource source = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();
            List<(string Method, string Path)> endpoints = [];

            foreach (RouteEndpoint endpoint in source.Endpoints.OfType<RouteEndpoint>())
            {
                bool isControllerAction = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null;
                bool isAnonymous = endpoint.Metadata.OfType<IAllowAnonymous>().Any();
                if (!isControllerAction || isAnonymous)
                {
                    continue;
                }

                string path = "/" + Regex.Replace(endpoint.RoutePattern.RawText ?? string.Empty, "\\{[^}]+\\}", _ => Guid.NewGuid().ToString());
                foreach (string method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])
                {
                    endpoints.Add((method, path));
                }
            }

            return endpoints;
        }
    }
}
