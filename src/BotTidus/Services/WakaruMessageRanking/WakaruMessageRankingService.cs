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

        static readonly Guid StampId_mareo_wakaruyo = new("57264a20-2240-49c7-910c-b04974591cc5");
        static readonly Guid StampId_devilman_wakaruman = new("6cce0b8d-f3c6-4285-8911-f4d5afac1fd0");
        static readonly Guid StampId_wakarunaa = new("3e629f09-dbd4-4c75-926a-6c52da092bfd");
        static readonly Guid StampId_wakaruman = new("148d2426-c0dc-4d81-b5fb-cfe59e9f798f");
        static readonly Guid StampId_wakaru_zubora = new("0fe81988-35ec-43df-9b7c-c93919a35fa3");
        static readonly Guid StampId_wakaru_tilted = new("01936952-9b8d-7fb6-ab44-d7cbdcbb4f36");
        static readonly Guid StampId_wakaru_spin = new("ee4f59ae-a054-4044-968a-914124bd16a9");
        static readonly Guid StampId_wakaru_parrot = new("8db1e759-858f-4ca4-9b98-961647416f81");
        static readonly Guid StampId_wakaru_basic = new("5cffff78-eed7-4a08-984f-dbce4ea6114d");
        static readonly Guid StampId_wakaru2 = new("a748dd2c-0aee-4aac-86f6-c584f4b979d5");
        static readonly Guid StampId_wakaru = new("1c891de7-e68c-4aa5-9cce-28f0ca74522c");
        static readonly Guid StampId_wakarimigaaru = new("01934910-1e35-713f-b79d-d8a9f03634b7");
        static readonly Guid StampId_wakarimi_kuroe = new("7c26349d-5050-4b9a-915c-bdd97ee5159d");
        static readonly Guid StampId_wakaranaidemonai = new("7c1559cb-b54e-4e4e-8b96-ceae6fe8fb9b");

        static readonly Guid StampId_mareo_imaha_madawakaranai = new("debb1dd3-7f2f-4424-b3a8-3d018e2bd73f");
        static readonly Guid StampId_wakarazu = new("e61e5dab-8e11-4959-849f-3bfdf9525d83");
        static readonly Guid StampId_wakaran_hanakappa = new("3410c49f-e41b-457c-83e6-b21677cf088e");
        static readonly Guid StampId_wakaran = new("b0f71031-0734-408d-96b8-b39d17cb0b3f");

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

            var top10Messages = messages.Select(m => (m.Id, GetWakaruScore(m))).OrderByDescending(m => m.Item2).Select(x => x.Id).Take(10);
            StringBuilder sb = new();
            foreach (var id in top10Messages)
            {
                sb.AppendLine($"https://q.trap.jp/messages/{id}");
            }

            await _traq.MessageApi.PostMessageAsync(_postChannelId, new(sb.ToString(), false), ct);
        }

        static int GetWakaruScore(Message message)
        {
            int score = 0;
            var stamps = message.Stamps.GroupBy(s => s.StampId).Select(g => (g.Key, g.Select(s => s.Count).Sum()));
            foreach (var (stampId, count) in stamps)
            {
                if (stampId == StampId_devilman_wakaruman
                    || stampId == StampId_mareo_wakaruyo
                    || stampId == StampId_wakarimigaaru
                    || stampId == StampId_wakarimi_kuroe
                    || stampId == StampId_wakaru
                    || stampId == StampId_wakaru2
                    || stampId == StampId_wakaruman
                    || stampId == StampId_wakarunaa
                    || stampId == StampId_wakaru_basic
                    || stampId == StampId_wakaru_parrot
                    || stampId == StampId_wakaru_spin
                    || stampId == StampId_wakaru_tilted
                    || stampId == StampId_wakaru_zubora)
                {
                    score += 2 * count;
                }
                else if (stampId == StampId_wakaranaidemonai)
                {
                    score += count;
                }
                else if (stampId == StampId_wakaran
                    || stampId == StampId_mareo_imaha_madawakaranai
                    || stampId == StampId_wakaran_hanakappa
                    || stampId == StampId_wakarazu)
                {
                    score -= 2 * count;
                }
            }
            return score;
        }
    }
}
