using System.Text.Json;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportPrivacyTests(ExportApp _app) : IClassFixture<ExportApp>
    {
        private const string PasswordHashPrefix = "AQAAAA";

        [Fact]
        public async Task Export_OfTheExporter_NeverMentionsTheOtherPersonsEmail()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            body.AssertDoesNotMention(scenario.Other.Form.Email);
        }

        [Fact]
        public async Task Export_OfTheExporter_NeverMentionsTheOtherPersonsPictureUrl()
        {
            await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            body.AssertDoesNotMention(ExportScenarioSteps.OtherPictureUrl);
            body.AssertDoesNotMention("googleusercontent");
        }

        [Fact]
        public async Task Export_OfTheExporter_NeverMentionsTheOtherPersonsUserId()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            body.AssertDoesNotMention(scenario.Other.UserId.ToString());
            body.AssertDoesNotMention(scenario.Other.UserId.ToString("N"));
        }

        [Fact]
        public async Task Export_OfTheExporter_NeverMentionsAnythingFromTheOtherPersonsPersonalBudget()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            string[] otherPersonalValues =
            [
                ExportScenarioSteps.OtherCategoryName,
                ExportScenarioSteps.OtherEntryNote,
                ExportScenarioSteps.OtherIncomeNote,
                ExportScenarioSteps.OtherSalaryTitle,
                scenario.OtherCategoryId.ToString(),
                scenario.OtherEntryId.ToString(),
                scenario.OtherIncomeEntryId.ToString(),
                scenario.OtherRecurringIncomeId.ToString(),
            ];
            foreach (string value in otherPersonalValues)
            {
                body.AssertDoesNotMention(value);
            }
        }

        [Fact]
        public async Task Export_OfTheExporter_NeverMentionsAnyPasswordHashOrSecurityStamp()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            foreach (Guid userId in new[] { scenario.Exporter.UserId, scenario.Other.UserId })
            {
                body.AssertDoesNotMention(await ExportRows.UserValueAsync(_app, userId, "password_hash"));
                body.AssertDoesNotMention(await ExportRows.UserValueAsync(_app, userId, "security_stamp"));
            }

            body.AssertDoesNotMention(PasswordHashPrefix);
        }

        [Fact]
        public async Task Export_OfTheExporter_NeverMentionsAnyInviteToken()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            foreach (Guid groupId in new[] { scenario.GroupId, scenario.LeftGroupId, scenario.DeletedGroupId, scenario.StrangersGroupId })
            {
                body.AssertDoesNotMention(await GroupRows.GroupValueAsync(_app, groupId, "invite_token") ?? string.Empty);
            }

            body.AssertDoesNotMention(scenario.PreviousInviteToken);
        }

        [Theory]
        [InlineData("passwordHash")]
        [InlineData("securityStamp")]
        [InlineData("lockout")]
        [InlineData("role")]
        [InlineData("claim")]
        [InlineData("token")]
        [InlineData("pictureUrl")]
        [InlineData("userId")]
        public async Task Export_OfTheExporter_HasNoFieldOrTextNamed(string forbiddenName)
        {
            await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            body.AssertDoesNotMention(forbiddenName);
        }

        [Fact]
        public async Task Export_OfTheOtherMember_ListsOnlyTheGroupsThatPersonIsIn()
        {
            ExportScenario scenario = await _app.ScenarioAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(scenario.Other.Client);

            Guid[] expected = [scenario.GroupId, scenario.LeftGroupId, scenario.StrangersGroupId];
            Assert.Equal(expected.Order(), ExportBody.IdsOf(body.Groups).Order());
        }

        [Fact]
        public async Task Export_OfTheOtherMember_ShowsTheirOwnProfileAndIsOwnerFalseInTheSharedGroup()
        {
            ExportScenario scenario = await _app.ScenarioAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(scenario.Other.Client);

            Assert.Equal(scenario.Other.UserId, body.Profile.GetProperty("id").GetGuid());
            Assert.Equal(scenario.Other.Form.Email, body.Profile.GetProperty("email").GetString());
            JsonElement sharedGroup = body.GroupWithId(scenario.GroupId);
            Assert.False(sharedGroup.GetProperty("isOwner").GetBoolean());
            Assert.Equal(scenario.Group.MemberRowId, sharedGroup.GetProperty("membership").GetProperty("memberId").GetGuid());
            JsonElement you = Assert.Single(ExportBody.ListOf(sharedGroup, "members"), member => member.GetProperty("isYou").GetBoolean());
            Assert.Equal(scenario.Group.MemberRowId, you.GetProperty("memberId").GetGuid());
        }

        [Fact]
        public async Task Export_OfTheOtherMember_ShowsTheirOwnPersonalBudget()
        {
            ExportScenario scenario = await _app.ScenarioAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(scenario.Other.Client);

            Assert.Equal([scenario.OtherCategoryId], ExportBody.IdsOf(ExportBody.ListOf(body.PersonalBudget, "categories")));
            Assert.Equal(
                new[] { scenario.OtherEntryId, scenario.OtherIncomeEntryId }.Order(),
                ExportBody.IdsOf(ExportBody.ListOf(body.PersonalBudget, "entries")).Order());
            Assert.Equal([scenario.OtherRecurringIncomeId], ExportBody.IdsOf(ExportBody.ListOf(body.PersonalBudget, "recurringIncomes")));
        }

        [Fact]
        public async Task Export_OfTheOtherMember_NeverMentionsTheExportersEmailUserIdOrPersonalBudget()
        {
            ExportScenario scenario = await _app.ScenarioAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(scenario.Other.Client);

            string[] exporterValues =
            [
                scenario.Exporter.Form.Email,
                scenario.Exporter.UserId.ToString(),
                scenario.Exporter.UserId.ToString("N"),
                ExportScenarioSteps.CoffeeCategoryName,
                ExportScenarioSteps.ArchivedCategoryName,
                ExportScenarioSteps.CoffeeNote,
                ExportScenarioSteps.BonusNote,
                ExportScenarioSteps.SalaryTitle,
                ExportScenarioSteps.StoppedIncomeTitle,
                scenario.CoffeeEntryId.ToString(),
                scenario.SalaryId.ToString(),
            ];
            foreach (string value in exporterValues)
            {
                body.AssertDoesNotMention(value);
            }
        }

        [Fact]
        public async Task Export_OfTheOtherMember_ShowsTheExportersFormerRowInTheGroupTheyLeftByDisplayName()
        {
            ExportScenario scenario = await _app.ScenarioAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(scenario.Other.Client);

            JsonElement leftGroup = body.GroupWithId(scenario.LeftGroupId);
            Assert.True(leftGroup.GetProperty("isOwner").GetBoolean());
            JsonElement exporterRow = Assert.Single(ExportBody.ListOf(leftGroup, "members"), member => member.GetProperty("endKind").ValueKind == JsonValueKind.String);
            Assert.Equal(scenario.Exporter.Form.DisplayName, exporterRow.GetProperty("displayName").GetString());
            Assert.Equal("Left", exporterRow.GetProperty("endKind").GetString());
            Assert.False(exporterRow.GetProperty("isYou").GetBoolean());
        }
    }
}
