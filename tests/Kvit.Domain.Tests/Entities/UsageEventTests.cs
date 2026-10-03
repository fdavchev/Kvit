using System.Globalization;
using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class UsageEventTests
    {
        private static readonly Guid UserId = Guid.Parse("0199a1b2-c3d4-7e5f-8a6b-7c8d9e0f1a2b");
        private static readonly DateTimeOffset SomeMoment = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

        [Theory]
        [InlineData(SignUpMethod.Email, "Email")]
        [InlineData(SignUpMethod.Google, "Google")]
        public void SignedUp_SetsTypeSignedUpAndTheMethodNameAsDetail(SignUpMethod method, string expectedDetail)
        {
            UsageEvent usageEvent = UsageEvent.SignedUp(UserId, method, SomeMoment);

            Assert.Equal(UsageEventType.SignedUp, usageEvent.Type);
            Assert.Equal(expectedDetail, usageEvent.Detail);
        }

        [Fact]
        public void SignedUp_KeepsTheUserId()
        {
            UsageEvent usageEvent = UsageEvent.SignedUp(UserId, SignUpMethod.Email, SomeMoment);

            Assert.Equal(UserId, usageEvent.UserId);
        }

        [Theory]
        [InlineData("2026-09-29T23:30:00-05:00", "2026-09-30T04:30:00+00:00")]
        [InlineData("2026-09-30T01:30:00+02:00", "2026-09-29T23:30:00+00:00")]
        [InlineData("2026-09-29T12:00:00+00:00", "2026-09-29T12:00:00+00:00")]
        public void SignedUp_StoresOccurredAtAsTheSameMomentAtOffsetZero(string occurredAtText, string expectedUtcText)
        {
            DateTimeOffset occurredAt = DateTimeOffset.Parse(occurredAtText, CultureInfo.InvariantCulture);
            DateTimeOffset expectedUtc = DateTimeOffset.Parse(expectedUtcText, CultureInfo.InvariantCulture);

            UsageEvent usageEvent = UsageEvent.SignedUp(UserId, SignUpMethod.Email, occurredAt);

            Assert.Equal(TimeSpan.Zero, usageEvent.OccurredAt.Offset);
            Assert.Equal(expectedUtc, usageEvent.OccurredAt);
        }

        [Theory]
        [InlineData("2026-09-29T23:30:00-05:00", "2026-09-30")]
        [InlineData("2026-09-30T01:30:00+02:00", "2026-09-29")]
        [InlineData("2026-09-29T23:59:59+00:00", "2026-09-29")]
        [InlineData("2026-09-30T00:00:00+00:00", "2026-09-30")]
        [InlineData("2026-12-31T20:00:00-05:00", "2027-01-01")]
        public void SignedUp_SetsOccurredOnToTheUtcCalendarDay(string occurredAtText, string expectedDayText)
        {
            DateTimeOffset occurredAt = DateTimeOffset.Parse(occurredAtText, CultureInfo.InvariantCulture);
            DateOnly expectedDay = DateOnly.Parse(expectedDayText, CultureInfo.InvariantCulture);

            UsageEvent usageEvent = UsageEvent.SignedUp(UserId, SignUpMethod.Email, occurredAt);

            Assert.Equal(expectedDay, usageEvent.OccurredOn);
        }

        [Fact]
        public void GroupCreated_SetsTypeGroupCreatedNoDetailAndTheUserId()
        {
            UsageEvent usageEvent = UsageEvent.GroupCreated(UserId, SomeMoment);

            Assert.Equal(UsageEventType.GroupCreated, usageEvent.Type);
            Assert.Null(usageEvent.Detail);
            Assert.Equal(UserId, usageEvent.UserId);
        }

        [Fact]
        public void JoinedViaInvite_SetsTypeJoinedViaInviteNoDetailAndTheUserId()
        {
            UsageEvent usageEvent = UsageEvent.JoinedViaInvite(UserId, SomeMoment);

            Assert.Equal(UsageEventType.JoinedViaInvite, usageEvent.Type);
            Assert.Null(usageEvent.Detail);
            Assert.Equal(UserId, usageEvent.UserId);
        }

        [Theory]
        [InlineData("2026-09-29T23:30:00-05:00", "2026-09-30")]
        [InlineData("2026-09-30T01:30:00+02:00", "2026-09-29")]
        [InlineData("2026-09-30T00:00:00+00:00", "2026-09-30")]
        public void JoinedViaInvite_SetsOccurredOnToTheUtcCalendarDayAndOccurredAtToOffsetZero(string occurredAtText, string expectedDayText)
        {
            DateTimeOffset occurredAt = DateTimeOffset.Parse(occurredAtText, CultureInfo.InvariantCulture);
            DateOnly expectedDay = DateOnly.Parse(expectedDayText, CultureInfo.InvariantCulture);

            UsageEvent usageEvent = UsageEvent.JoinedViaInvite(UserId, occurredAt);

            Assert.Equal(expectedDay, usageEvent.OccurredOn);
            Assert.Equal(TimeSpan.Zero, usageEvent.OccurredAt.Offset);
            Assert.Equal(occurredAt, usageEvent.OccurredAt);
        }

        [Theory]
        [InlineData("2026-09-29T23:30:00-05:00", "2026-09-30")]
        [InlineData("2026-09-30T01:30:00+02:00", "2026-09-29")]
        [InlineData("2026-09-30T00:00:00+00:00", "2026-09-30")]
        public void GroupCreated_SetsOccurredOnToTheUtcCalendarDayAndOccurredAtToOffsetZero(string occurredAtText, string expectedDayText)
        {
            DateTimeOffset occurredAt = DateTimeOffset.Parse(occurredAtText, CultureInfo.InvariantCulture);
            DateOnly expectedDay = DateOnly.Parse(expectedDayText, CultureInfo.InvariantCulture);

            UsageEvent usageEvent = UsageEvent.GroupCreated(UserId, occurredAt);

            Assert.Equal(expectedDay, usageEvent.OccurredOn);
            Assert.Equal(TimeSpan.Zero, usageEvent.OccurredAt.Offset);
            Assert.Equal(occurredAt, usageEvent.OccurredAt);
        }
    }
}
