using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public class ClosingExpiryWriteTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int JustInsideTheWindowSeconds = (24 * 60 * 60) - 1;
        private const int ExactlyTheWindowSeconds = 24 * 60 * 60;
        private const int TwoDaysSeconds = 2 * 24 * 60 * 60;

        [Fact]
        public async Task Confirm_OneSecondBefore24Hours_StillWorks()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(JustInsideTheWindowSeconds));

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(group.Member.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal(1, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_ByTheLastMemberOneSecondBefore24Hours_FinishesTheGroupWithTheClockTime()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            _app.Clock.Advance(TimeSpan.FromSeconds(JustInsideTheWindowSeconds));

            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);

            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await ClosingRows.FinishedAtUtcAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData(ExactlyTheWindowSeconds)]
        [InlineData(TwoDaysSeconds)]
        public async Task Confirm_After24Hours_Answers400GroupNotClosingAndStoresNothing(int secondsSinceStart)
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(secondsSinceStart));

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_CLOSING);
            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Object_OneSecondBefore24Hours_StillWorks()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(JustInsideTheWindowSeconds));

            await ClosingSteps.ObjectAsync(group.Member, group.GroupId, "Just in time");

            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData(ExactlyTheWindowSeconds)]
        [InlineData(TwoDaysSeconds)]
        public async Task Object_After24Hours_Answers400GroupNotClosingAndWritesNoObjectedEvent(int secondsSinceStart)
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(secondsSinceStart));

            HttpResponseMessage response = await ClosingRequests.ObjectAsync(group.Member.Client, group.GroupId, "Too late");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_CLOSING);
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingObjected"));
        }

        [Theory]
        [InlineData(ExactlyTheWindowSeconds)]
        [InlineData(TwoDaysSeconds)]
        public async Task Unlock_After24Hours_TreatsTheGroupAsFinishedSavesItAndThenReopensIt(int secondsSinceStart)
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(secondsSinceStart));

            HttpResponseMessage response = await ClosingRequests.UnlockAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "closing_started_at"));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "finished_at"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupReopened"));
        }

        [Fact]
        public async Task Unlock_OneSecondBefore24Hours_Answers400GroupNotFinishedAndKeepsTheGroupClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(JustInsideTheWindowSeconds));

            HttpResponseMessage response = await ClosingRequests.UnlockAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_FINISHED);
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Start_After24HoursOfAnUnansweredClosing_Answers400GroupNotOpenBecauseTheGroupCountsAsFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(ExactlyTheWindowSeconds));

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_OPEN);
        }

        [Fact]
        public async Task Start_ByAMemberAfter24Hours_Answers403GroupNotOwnerBeforeTheStateIsChecked()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(ExactlyTheWindowSeconds));

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Start_AfterTheOwnerUnlockedAnExpiredClosing_StartsAFreshClosingFromTheClockTime()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(TimeSpan.FromSeconds(ExactlyTheWindowSeconds));
            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await ClosingRows.ClosingStartedAtUtcAsync(_app, group.GroupId));
            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_ByAStrangerAfter24Hours_Answers404GroupNotFoundBeforeTheStateIsChecked()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            _app.Clock.Advance(TimeSpan.FromSeconds(ExactlyTheWindowSeconds));

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(stranger.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }
    }
}
