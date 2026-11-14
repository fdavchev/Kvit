using System.Text.Json;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeAnswersTests(AccountDeletionApp _app) : IClassFixture<AccountDeletionApp>
    {
        [Theory]
        [InlineData("groups")]
        [InlineData("group")]
        [InlineData("members")]
        [InlineData("expenses")]
        [InlineData("expense")]
        [InlineData("deleted-expenses")]
        [InlineData("activity")]
        [InlineData("balances")]
        [InlineData("settlements")]
        [InlineData("dashboard")]
        public async Task Answer_AfterAFormerMemberPurge_NeverContainsTheOldName(string answer)
        {
            DeletedPersonScenario scenario = await _app.FormerMemberAsync();

            string text = await DeletedPersonSteps.AnswerTextOfAsync(scenario, answer);

            Assert.DoesNotContain(scenario.OldName, text);
        }

        [Theory]
        [InlineData("members")]
        [InlineData("expense")]
        [InlineData("activity")]
        [InlineData("balances")]
        [InlineData("settlements")]
        [InlineData("dashboard")]
        public async Task Answer_AfterAFormerMemberPurge_ShowsFormerMemberWhereThePersonAppears(string answer)
        {
            DeletedPersonScenario scenario = await _app.FormerMemberAsync();

            string text = await DeletedPersonSteps.AnswerTextOfAsync(scenario, answer);

            Assert.Contains(DeletionRequests.FormerMemberEnglish, text);
        }

        [Theory]
        [InlineData("members")]
        [InlineData("expense")]
        [InlineData("activity")]
        [InlineData("balances")]
        [InlineData("settlements")]
        [InlineData("dashboard")]
        public async Task Answer_AfterAKeepNamePurge_StillShowsTheOldName(string answer)
        {
            DeletedPersonScenario scenario = await _app.KeepNameAsync();

            string text = await DeletedPersonSteps.AnswerTextOfAsync(scenario, answer);

            Assert.Contains(scenario.OldName, text);
        }

        [Theory]
        [InlineData("members")]
        [InlineData("expense")]
        [InlineData("activity")]
        [InlineData("balances")]
        [InlineData("settlements")]
        [InlineData("dashboard")]
        public async Task Answer_AfterAKeepNamePurge_NeverShowsFormerMember(string answer)
        {
            DeletedPersonScenario scenario = await _app.KeepNameAsync();

            string text = await DeletedPersonSteps.AnswerTextOfAsync(scenario, answer);

            Assert.DoesNotContain(DeletionRequests.FormerMemberEnglish, text);
        }

        [Fact]
        public async Task ExpenseDetail_AfterAFormerMemberPurge_ShowsFormerMemberAsCreatedByAndInTheHistory()
        {
            DeletedPersonScenario scenario = await _app.FormerMemberAsync();

            string text = await DeletedPersonSteps.AnswerTextOfAsync(scenario, "expense");

            using JsonDocument document = JsonDocument.Parse(text);
            Assert.Equal(DeletionRequests.FormerMemberEnglish, document.RootElement.GetProperty("createdByName").GetString());
            Assert.Contains(document.RootElement.GetProperty("history").EnumerateArray(), entry => entry.GetProperty("actorName").GetString() == DeletionRequests.FormerMemberEnglish);
        }

        [Fact]
        public async Task ExpenseDetail_AfterAKeepNamePurge_ShowsTheOldNameAsCreatedBy()
        {
            DeletedPersonScenario scenario = await _app.KeepNameAsync();

            string text = await DeletedPersonSteps.AnswerTextOfAsync(scenario, "expense");

            using JsonDocument document = JsonDocument.Parse(text);
            Assert.Equal(scenario.OldName, document.RootElement.GetProperty("createdByName").GetString());
        }

        [Fact]
        public async Task Activity_AfterAFormerMemberPurge_ShowsFormerMemberAsTheActorOfThePersonsEvents()
        {
            DeletedPersonScenario scenario = await _app.FormerMemberAsync();

            string text = await DeletedPersonSteps.AnswerTextOfAsync(scenario, "activity");

            using JsonDocument document = JsonDocument.Parse(text);
            Assert.Contains(document.RootElement.GetProperty("events").EnumerateArray(), entry => entry.GetProperty("actorUserId").GetGuid() == scenario.Leaver.UserId && entry.GetProperty("actorName").GetString() == DeletionRequests.FormerMemberEnglish);
        }

        [Fact]
        public async Task Groups_AfterAFormerMemberPurge_TheOwnerStillSeesTheGroupWithTheSameMembers()
        {
            DeletedPersonScenario scenario = await _app.FormerMemberAsync();

            string text = await DeletedPersonSteps.AnswerTextOfAsync(scenario, "group");

            using JsonDocument document = JsonDocument.Parse(text);
            Assert.Equal(scenario.Group.RowIdsInJoiningOrder.Count, document.RootElement.GetProperty("memberCount").GetInt32());
        }
    }
}
