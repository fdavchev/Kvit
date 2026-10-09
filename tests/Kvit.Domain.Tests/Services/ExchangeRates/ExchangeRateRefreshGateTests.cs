using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Tests.Fakes;
using Xunit;

namespace Kvit.Domain.Tests.Services.ExchangeRates
{
    public class ExchangeRateRefreshGateTests
    {
        private readonly AdjustableTimeProvider _time = new(ExchangeRateTestData.Now);
        private readonly ExchangeRateRefreshGate _gate;

        public ExchangeRateRefreshGateTests()
        {
            _gate = new ExchangeRateRefreshGate(_time);
        }

        [Fact]
        public void CanTry_AtTheStart_IsTrue()
        {
            bool canTry = _gate.CanTry();

            Assert.True(canTry);
        }

        [Fact]
        public void CanTry_LongAfterTheStartWithNoFailure_IsStillTrue()
        {
            _time.Advance(TimeSpan.FromDays(3));

            bool canTry = _gate.CanTry();

            Assert.True(canTry);
        }

        [Fact]
        public void CanTry_RightAfterAFailure_IsFalse()
        {
            _gate.RecordFailure();

            bool canTry = _gate.CanTry();

            Assert.False(canTry);
        }

        [Fact]
        public void CanTry_29Minutes59SecondsAfterAFailure_IsFalse()
        {
            _gate.RecordFailure();
            _time.Advance(TimeSpan.FromMinutes(29) + TimeSpan.FromSeconds(59));

            bool canTry = _gate.CanTry();

            Assert.False(canTry);
        }

        [Fact]
        public void CanTry_30Minutes1SecondAfterAFailure_IsTrue()
        {
            _gate.RecordFailure();
            _time.Advance(TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(1));

            bool canTry = _gate.CanTry();

            Assert.True(canTry);
        }

        [Fact]
        public void CanTry_AskedTwiceInsideTheWaitingTime_StaysFalseBothTimes()
        {
            _gate.RecordFailure();
            _time.Advance(TimeSpan.FromMinutes(10));

            bool first = _gate.CanTry();
            bool second = _gate.CanTry();

            Assert.False(first);
            Assert.False(second);
        }

        [Fact]
        public void RecordFailure_AfterTheFirstWaitEnded_StartsANewThirtyMinuteWait()
        {
            _gate.RecordFailure();
            _time.Advance(TimeSpan.FromMinutes(31));
            _gate.RecordFailure();
            _time.Advance(TimeSpan.FromMinutes(29));

            bool canTry = _gate.CanTry();

            Assert.False(canTry);
        }

        [Fact]
        public void RecordFailure_TwiceInsideTheWait_RestartsTheWaitFromTheSecondFailure()
        {
            _gate.RecordFailure();
            _time.Advance(TimeSpan.FromMinutes(20));
            _gate.RecordFailure();
            _time.Advance(TimeSpan.FromMinutes(20));

            bool canTry = _gate.CanTry();

            Assert.False(canTry);
        }
    }
}
