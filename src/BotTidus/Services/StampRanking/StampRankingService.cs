using BotTidus.Helpers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Traq;
using Traq.Model;

namespace BotTidus.Services.StampRanking
{
    internal class StampRankingService(
        ILogger<StampRankingService> logger,
        IOptions<AppConfig> appConfig,
        ITraqApiClient traq,
        TimeZoneInfo timeZoneInfo,
        IServiceProvider services
        )
        : DailyMessageCollectingService(services, TimeHelper.GetTimeSpanUntilNextTime(TimeOnly.MinValue)), // Wait until next 09:00:00(JST)
          IHealthCheck
    {
        readonly Guid _postChannelId = appConfig.Value.StampRankingChannelId;

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
            var stampNameMap = (await traq.StampApi.GetStampsAsync(cancellationToken: ct)).ToDictionary(s => s.Id, s => s.Name);
            var stampCount = messages.SelectMany(m => m.Stamps).GroupBy(s => s.StampId).ToDictionary(g => g.Key, ms => ms.Sum(s => s.Count));
            var top50stamps = stampCount.OrderByDescending(kv => kv.Value).TakeWhile(kv => kv.Value > 0).Take(50);

            var yesterday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo).Date.AddDays(-1);
            StringBuilder sb = new($"""
                    ## {yesterday:M/d} Stamp Ranking 50 だよ！
                    | rank | stamp | count |
                    |-----:|:------|------:|
                    """);
            sb.AppendLine();

            int prevCount = int.MaxValue;
            int rank = 1;
            foreach (var (stampId, count) in top50stamps)
            {
                if (!stampNameMap.TryGetValue(stampId, out var stampName))
                {
                    logger.LogWarning("Unknown stamp: {}", stampId);
                    continue;
                }
                sb.AppendLine(prevCount == count
                    ? $"| - | :{stampName}: `{stampName}` | {count} |"
                    : $"| {rank} | :{stampName}: `{stampName}` | {count} |");
                prevCount = count;
                rank++;
            }

            await traq.MessageApi.PostMessageAsync(_postChannelId, new PostMessageRequest(sb.ToString(), false), ct);
        }
    }
}
