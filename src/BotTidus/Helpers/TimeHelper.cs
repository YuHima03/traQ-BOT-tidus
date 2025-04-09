namespace BotTidus.Helpers
{
    internal static class TimeHelper
    {
        public static TimeSpan GetTimeSpanUntilNextTime(TimeOnly utcTime)
        {
            TimeOnly utcNowTime = TimeOnly.FromDateTime(DateTime.UtcNow);
            var diff = utcTime - utcNowTime;
            if (diff < TimeSpan.Zero)
            {
                diff += TimeSpan.FromDays(1);
            }
            return diff;
        }
    }
}
