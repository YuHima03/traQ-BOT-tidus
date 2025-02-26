using Traq.Model;

namespace BotTidus.Services
{
    internal sealed class FaceCollectingService(IServiceProvider services) : RecentMessageCollectingService(services, TimeSpan.FromSeconds(30))
    {
        protected override async ValueTask OnCollectAsync(ActivityTimelineMessage[] messages, CancellationToken ct)
        {
            return;
        }
    }
}
