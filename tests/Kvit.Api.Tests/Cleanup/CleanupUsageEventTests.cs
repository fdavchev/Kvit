using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupUsageEventTests(CleanupApp app) : CleanupTestBase(app)
    {
        private const string AnyType = "GroupCreated";

        public static TheoryData<string> EveryEventType =>
        [
            "SignedUp",
            "Active",
            "JoinedViaInvite",
            "GroupCreated",
            "ExpenseAdded",
            "SettlementConfirmed",
            "GuestUpgraded",
        ];

        private DateTimeOffset TwelveMonthsAgo
        {
            get { return _app.Now.AddMonths(-12); }
        }

        [Fact]
        public async Task Post_EventOneSecondOlderThanTwelveMonths_LosesItsLinkToTheAccount()
        {
            SignedInUser user = await _app.SignUpAsync();
            long eventId = await CleanupRows.InsertUsageEventAsync(_app, user.UserId, AnyType, TwelveMonthsAgo - CleanupSteps.OneSecond);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { UsageEventsDetached = 1 }, result);
            Assert.Null(await CleanupRows.UsageEventUserIdAsync(_app, eventId));
        }

        [Fact]
        public async Task Post_EventExactlyTwelveMonthsOld_StaysLinked()
        {
            SignedInUser user = await _app.SignUpAsync();
            long eventId = await CleanupRows.InsertUsageEventAsync(_app, user.UserId, AnyType, TwelveMonthsAgo);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
            Assert.Equal(user.UserId, await CleanupRows.UsageEventUserIdAsync(_app, eventId));
        }

        [Fact]
        public async Task Post_EventOneSecondYoungerThanTwelveMonths_StaysLinked()
        {
            SignedInUser user = await _app.SignUpAsync();
            long eventId = await CleanupRows.InsertUsageEventAsync(_app, user.UserId, AnyType, TwelveMonthsAgo + CleanupSteps.OneSecond);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
            Assert.Equal(user.UserId, await CleanupRows.UsageEventUserIdAsync(_app, eventId));
        }

        [Fact]
        public async Task Post_EventElevenMonthsOld_StaysLinked()
        {
            SignedInUser user = await _app.SignUpAsync();
            long eventId = await CleanupRows.InsertUsageEventAsync(_app, user.UserId, AnyType, _app.Now.AddMonths(-11));

            await _app.RunCleanupAsync();

            Assert.Equal(user.UserId, await CleanupRows.UsageEventUserIdAsync(_app, eventId));
        }

        [Theory]
        [MemberData(nameof(EveryEventType))]
        public async Task Post_OldEventOfAnyType_LosesItsLinkToTheAccount(string type)
        {
            SignedInUser user = await _app.SignUpAsync();
            long eventId = await CleanupRows.InsertUsageEventAsync(_app, user.UserId, type, TwelveMonthsAgo - CleanupSteps.OneSecond);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(1, result.UsageEventsDetached);
            Assert.Null(await CleanupRows.UsageEventUserIdAsync(_app, eventId));
        }

        [Fact]
        public async Task Post_TwoOldActiveEventsOfOnePerson_BothLoseTheirLink()
        {
            SignedInUser user = await _app.SignUpAsync();
            long first = await CleanupRows.InsertUsageEventAsync(_app, user.UserId, "Active", _app.Now.AddMonths(-14));
            long second = await CleanupRows.InsertUsageEventAsync(_app, user.UserId, "Active", _app.Now.AddMonths(-13));

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(2, result.UsageEventsDetached);
            Assert.Null(await CleanupRows.UsageEventUserIdAsync(_app, first));
            Assert.Null(await CleanupRows.UsageEventUserIdAsync(_app, second));
        }

        [Fact]
        public async Task Post_DetachedEvent_KeepsItsRowTypeDetailAndDates()
        {
            SignedInUser user = await _app.SignUpAsync();
            long eventId = await CleanupRows.InsertUsageEventAsync(_app, user.UserId, "SignedUp", TwelveMonthsAgo - TimeSpan.FromDays(40), "Google");
            string before = await CleanupRows.UsageEventContentAsync(_app, eventId);

            await _app.RunCleanupAsync();

            Assert.Equal(before, await CleanupRows.UsageEventContentAsync(_app, eventId));
        }

        [Fact]
        public async Task Post_Detaching_TheNumberOfUsageEventsStaysTheSame()
        {
            SignedInUser user = await _app.SignUpAsync();
            await CleanupRows.InsertUsageEventAsync(_app, user.UserId, AnyType, _app.Now.AddMonths(-20));
            await CleanupRows.InsertUsageEventAsync(_app, user.UserId, "ExpenseAdded", _app.Now.AddMonths(-15));
            int before = await CleanupRows.CountUsageEventsAsync(_app);

            await _app.RunCleanupAsync();

            Assert.Equal(before, await CleanupRows.CountUsageEventsAsync(_app));
        }

        [Fact]
        public async Task Post_OldEventThatHasNoPersonAnyMore_IsNotCounted()
        {
            SignedInUser user = await _app.SignUpAsync();
            await CleanupRows.InsertUsageEventAsync(_app, null, AnyType, _app.Now.AddMonths(-20));
            await CleanupRows.InsertUsageEventAsync(_app, null, "ExpenseAdded", _app.Now.AddMonths(-30));
            await CleanupRows.InsertUsageEventAsync(_app, user.UserId, AnyType, _app.Now.AddMonths(-20));

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(1, result.UsageEventsDetached);
        }

        [Fact]
        public async Task Post_OldEventsOfThreePeople_CountsEachEventOnce()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync();
            SignedInUser third = await _app.SignUpAsync();
            foreach (SignedInUser person in new[] { first, second, third })
            {
                await CleanupRows.InsertUsageEventAsync(_app, person.UserId, AnyType, _app.Now.AddMonths(-13));
                await CleanupRows.InsertUsageEventAsync(_app, person.UserId, "ExpenseAdded", _app.Now.AddMonths(-18));
            }

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { UsageEventsDetached = 6 }, result);
        }

        [Fact]
        public async Task Post_YoungEventsOfAPersonWithOldEvents_StayLinked()
        {
            SignedInUser user = await _app.SignUpAsync();
            long oldId = await CleanupRows.InsertUsageEventAsync(_app, user.UserId, AnyType, _app.Now.AddMonths(-13));
            long youngId = await CleanupRows.InsertUsageEventAsync(_app, user.UserId, "ExpenseAdded", _app.Now.AddMonths(-2));

            await _app.RunCleanupAsync();

            Assert.Null(await CleanupRows.UsageEventUserIdAsync(_app, oldId));
            Assert.Equal(user.UserId, await CleanupRows.UsageEventUserIdAsync(_app, youngId));
        }

        [Fact]
        public async Task Post_YoungEventsOfOtherPeople_AreUntouched()
        {
            SignedInUser oldPerson = await _app.SignUpAsync();
            SignedInUser youngPerson = await _app.SignUpAsync();
            await CleanupRows.InsertUsageEventAsync(_app, oldPerson.UserId, AnyType, _app.Now.AddMonths(-13));
            long youngId = await CleanupRows.InsertUsageEventAsync(_app, youngPerson.UserId, "ExpenseAdded", _app.Now.AddDays(-3));
            string youngBefore = await CleanupRows.UsageEventContentAsync(_app, youngId);

            await _app.RunCleanupAsync();

            Assert.Equal(youngPerson.UserId, await CleanupRows.UsageEventUserIdAsync(_app, youngId));
            Assert.Equal(youngBefore, await CleanupRows.UsageEventContentAsync(_app, youngId));
        }

        [Fact]
        public async Task Post_PersonWhoseEventsWereDetached_CanStillUseTheAccount()
        {
            SignedInUser user = await _app.SignUpAsync();
            await CleanupRows.InsertUsageEventAsync(_app, user.UserId, AnyType, _app.Now.AddMonths(-13));

            await _app.RunCleanupAsync();

            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(user.Client)).StatusCode);
        }

        [Fact]
        public async Task Post_TheAppClockDecidesWhatIsOld_NotTheRealClock()
        {
            SignedInUser user = await _app.SignUpAsync();
            int eventsOfThePerson = (await _app.UsageEventsOfAsync(user.UserId)).Count;
            Assert.True(eventsOfThePerson > 0, "Signing up should have written a usage event.");
            _app.Clock.SetTo(_app.Now.AddMonths(12) + CleanupSteps.OneSecond);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(eventsOfThePerson, result.UsageEventsDetached);
            Assert.Empty(await _app.UsageEventsOfAsync(user.UserId));
        }
    }
}
