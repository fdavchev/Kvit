using System.Globalization;

namespace Kvit.Api.Tests.AdminStats
{
    public static class Moment
    {
        public static DateTimeOffset At(string utcText)
        {
            return DateTimeOffset.Parse(utcText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        }
    }
}
