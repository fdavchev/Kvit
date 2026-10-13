using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public class ObjectToClosingTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string Reason = "I still owe Marko 200 for the taxi";

        [Fact]
        public async Task Object_WithAReason_Answers204WithNoBody()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            HttpResponseMessage response = await ClosingRequests.ObjectAsync(group.Member.Client, group.GroupId, Reason);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Object_ByAMember_ReturnsTheGroupToOpenAndClearsTheClosingTimeWithoutFinishing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            await ClosingSteps.ObjectAsync(group.Member, group.GroupId, Reason);

            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "closing_started_at"));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "finished_at"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
        }

        [Fact]
        public async Task Object_AfterAnotherMemberConfirmed_DeletesEveryResponseOfTheGroup()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            Assert.Equal(1, await ClosingResponseRows.CountAsync(_app, group.GroupId));

            await ClosingSteps.ObjectAsync(group.Third, group.GroupId, Reason);

            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, group.GroupId));
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Object_InOneGroup_KeepsTheResponsesOfAnotherGroup()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            SettlementGroup other = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(other.Member, other.GroupId);

            await ClosingSteps.ObjectAsync(group.Member, group.GroupId, Reason);

            Assert.Equal(1, await ClosingResponseRows.CountAsync(_app, other.GroupId));
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, other.GroupId));
        }

        [Fact]
        public async Task Object_WithAReason_WritesOneObjectedEventByTheMemberWithTheReasonInTheData()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            await ClosingSteps.ObjectAsync(group.Member, group.GroupId, Reason);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingObjected"));
            Assert.Equal(group.Member.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingObjected", "actor_user_id"));
            Assert.Equal(Reason, await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingObjected", "data->>'reason'"));
        }

        [Fact]
        public async Task Object_ReasonWithSpacesAroundIt_IsTrimmedInTheEventData()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            await ClosingSteps.ObjectAsync(group.Member, group.GroupId, "   too early   ");

            Assert.Equal("too early", await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingObjected", "data->>'reason'"));
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"reason\":null}")]
        public async Task Object_WithoutAReason_Answers204AndWritesTheEventWithNoReason(string json)
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            HttpResponseMessage response = await ClosingRequests.ObjectWithJsonAsync(group.Member.Client, group.GroupId, json);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingObjected"));
            Assert.Null(await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingObjected", "data->>'reason'"));
        }

        [Fact]
        public async Task Object_ReasonOfExactly200Characters_IsAccepted()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            string reason = ExpenseInputs.TextOfLength(200);

            await ClosingSteps.ObjectAsync(group.Member, group.GroupId, reason);

            Assert.Equal(reason, await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingObjected", "data->>'reason'"));
        }

        [Fact]
        public async Task Object_ReasonOf200CharactersPlusSpaces_IsAcceptedBecauseItIsTrimmedBeforeTheLengthIsChecked()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            string reason = ExpenseInputs.TextOfLength(200);

            await ClosingSteps.ObjectAsync(group.Member, group.GroupId, $"  {reason}  ");

            Assert.Equal(reason, await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingObjected", "data->>'reason'"));
        }

        [Fact]
        public async Task Object_ReasonOf200CyrillicLetters_IsAcceptedBecauseTheLimitCountsCharactersNotBytes()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            string reason = new('а', 200);

            await ClosingSteps.ObjectAsync(group.Member, group.GroupId, reason);

            Assert.Equal(reason, await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingObjected", "data->>'reason'"));
        }

        [Fact]
        public async Task Object_ThenTheOwnerStartsAgain_EveryMemberHasToConfirmAgain()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            await ClosingSteps.ObjectAsync(group.Third, group.GroupId, Reason);
            _app.Clock.Advance(ClosingSteps.OneMinute);

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);

            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(1, await ClosingResponseRows.CountAsync(_app, group.GroupId));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await ClosingRows.ClosingStartedAtUtcAsync(_app, group.GroupId));
        }
    }
}
