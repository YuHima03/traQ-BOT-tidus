using BotTidus.Helpers;
using Traq.Model;

namespace BotTidus.Services.WakaruMessageRanking
{
    internal class WakaruMessageRankingService(IServiceProvider services)
        : DailyMessageCollectingService(services, TimeHelper.GetTimeSpanUntilNextTime(TimeOnly.MinValue)) // Wait until next 09:00:00(JST)
    {
        protected override ValueTask OnCollectAsync(Message[] messages, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
