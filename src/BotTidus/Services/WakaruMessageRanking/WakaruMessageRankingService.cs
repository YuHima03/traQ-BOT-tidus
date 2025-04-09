using BotTidus.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Traq;
using Traq.Model;

namespace BotTidus.Services.WakaruMessageRanking
{
    internal class WakaruMessageRankingService(IServiceProvider services)
        : DailyMessageCollectingService(services, TimeHelper.GetTimeSpanUntilNextTime(TimeOnly.MinValue)), // Wait until next 09:00:00(JST)
          IHealthCheck
    {
        readonly ILogger<WakaruMessageRankingService> _logger = services.GetRequiredService<ILogger<WakaruMessageRankingService>>();
        readonly Guid _postChannelId = services.GetService<IOptions<AppConfig>>()?.Value.WakaruMessageRankingChannelId ?? Guid.Empty;
        readonly ITraqApiClient _traq = services.GetRequiredService<ITraqApiClient>();

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_postChannelId == Guid.Empty)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Channel to post is not set."));
            }
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        protected override async ValueTask OnCollectAsync(Message[] messages, CancellationToken ct)
        {
            if (_postChannelId == Guid.Empty)
            {
                _logger.LogWarning("Channel to post is not set.");
                return;
            }
            var postMessage = await _traq.MessageApi.PostMessageAsync(_postChannelId, new PostMessageRequest(":loading: Collecting message stamps...", false), ct);
            var top10Messages = messages.Select(m => (m.Id, GetWakaruScore(m))).Where(m => m.Item2 != 0).OrderByDescending(m => m.Item2).Select(x => x.Id).Take(10);
            StringBuilder sb = new();
            foreach (var id in top10Messages)
            {
                sb.AppendLine($"https://q.trap.jp/messages/{id}");
            }
            await _traq.MessageApi.EditMessageAsync(postMessage.Id, new PostMessageRequest(sb.ToString(), false), ct);
        }

        static int GetWakaruScore(Message message)
        {
            int score = 0;
            var stamps = message.Stamps.GroupBy(s => s.UserId).Select(g => (g.Key, g.Select(s => s.StampId))); // Count the number of users who used each stamp.
            foreach (var (u, s) in stamps)
            {
                var sortedScores = s.Select(StampScoreExtension.GetStampScore).Where(sc => sc != 0).Order();
                var min = sortedScores.FirstOrDefault();
                score += (min < 0) ? min : sortedScores.LastOrDefault();
            }
            return score;
        }
    }

    file static class StampScoreExtension
    {
        static readonly Guid[] PositiveStamps = [
            new("57264a20-2240-49c7-910c-b04974591cc5"), // mareo_wakaruyo
            new("6cce0b8d-f3c6-4285-8911-f4d5afac1fd0"), // devilman_wakaruman
            new("3e629f09-dbd4-4c75-926a-6c52da092bfd"), // wakarunaa
            new("148d2426-c0dc-4d81-b5fb-cfe59e9f798f"), // wakaruman
            new("0fe81988-35ec-43df-9b7c-c93919a35fa3"), // wakaru_zubora
            new("01936952-9b8d-7fb6-ab44-d7cbdcbb4f36"), // wakaru_tilted
            new("ee4f59ae-a054-4044-968a-914124bd16a9"), // wakaru_spin
            new("8db1e759-858f-4ca4-9b98-961647416f81"), // wakaru_parrot
            new("5cffff78-eed7-4a08-984f-dbce4ea6114d"), // wakaru_basic
            new("a748dd2c-0aee-4aac-86f6-c584f4b979d5"), // wakaru2
            new("1c891de7-e68c-4aa5-9cce-28f0ca74522c"), // wakaru
            new("01934910-1e35-713f-b79d-d8a9f03634b7"), // wakarimigaaru
            new("7c26349d-5050-4b9a-915c-bdd97ee5159d"), // wakarimi_kuroe
            ];

        static readonly Guid[] SoftPositiveStamps = [
            new("7c1559cb-b54e-4e4e-8b96-ceae6fe8fb9b"), // wakaranaidemonai
            ];

        static readonly Guid[] NegativeStamps = [
            new("debb1dd3-7f2f-4424-b3a8-3d018e2bd73f"), // mareo_imaha_madawakaranai
            new("e61e5dab-8e11-4959-849f-3bfdf9525d83"), // wakarazu
            new("3410c49f-e41b-457c-83e6-b21677cf088e"), // wakaran_hanakappa
            new("b0f71031-0734-408d-96b8-b39d17cb0b3f"), // wakaran
            ];

        public static int GetStampScore(this Guid stampId)
        {
            if (PositiveStamps.AsSpan().Contains(stampId))
            {
                return 2;
            }
            else if (SoftPositiveStamps.AsSpan().Contains(stampId))
            {
                return 1;
            }
            else if (NegativeStamps.AsSpan().Contains(stampId))
            {
                return -2;
            }
            return 0;
        }
    }
}
