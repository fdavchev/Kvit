using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportShapeTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Export_TopLevelObject_HasExactlyThePinnedFieldNames()
        {
            SignedInUser user = await _app.SignUpAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.Equal(ExportRequests.TopLevelFieldNames, GroupRequests.PropertyNamesOf(body.Root));
        }

        [Fact]
        public async Task Export_Profile_HasExactlyThePinnedFieldNames()
        {
            SignedInUser user = await _app.SignUpAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.Equal(ExportRequests.ProfileFieldNames, GroupRequests.PropertyNamesOf(body.Profile));
        }

        [Fact]
        public async Task Export_PersonalBudget_HasExactlyThePinnedFieldNames()
        {
            SignedInUser user = await _app.SignUpAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.Equal(ExportRequests.PersonalBudgetFieldNames, GroupRequests.PropertyNamesOf(body.PersonalBudget));
        }

        [Fact]
        public async Task Export_PrivacyNotice_IsTheLinkToThePrivacyPage()
        {
            SignedInUser user = await _app.SignUpAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.Equal(ExportRequests.PrivacyNoticeUrl, body.Root.GetProperty("privacyNotice").GetString());
        }

        [Fact]
        public async Task Export_PersonWithNothing_AnswersEmptyGroups()
        {
            SignedInUser user = await _app.SignUpAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.Empty(body.Groups);
        }

        [Theory]
        [InlineData("categories")]
        [InlineData("entries")]
        [InlineData("recurringIncomes")]
        [InlineData("budgetLimits")]
        [InlineData("duplicateAnswers")]
        public async Task Export_PersonWithNothing_AnswersEmptyPersonalBudgetLists(string listName)
        {
            SignedInUser user = await _app.SignUpAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.Empty(ExportBody.ListOf(body.PersonalBudget, listName));
        }
    }
}
