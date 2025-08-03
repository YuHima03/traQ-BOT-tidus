using BotTidus.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Traq;
using Traq.Models;

namespace BotTidus.Services.StampRanking
{
    internal class StampRankingService(
        ILogger<StampRankingService> logger,
        IOptions<StampRankingServiceOptions> options,
        TraqApiClient traq,
        TimeZoneInfo timeZoneInfo,
        IServiceProvider services
        )
        : DailyMessageCollectingService(services, TimeHelper.GetTimeSpanUntilNextTime(TimeOnly.MinValue)), // Wait until next 09:00:00(JST)
          IHealthCheck
    {
        readonly Guid _postChannelId = options.Value.PostChannelId;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_postChannelId == Guid.Empty)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Channel to post is not set."));
            }
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        protected override async ValueTask OnCollectAsync(IList<Message> messages, CancellationToken ct)
        {
            var stampNameMap = (await traq.Stamps.GetAsync(cancellationToken: ct) ?? []).ToDictionary(s => s.Id!.Value, s => s.Name!);
            var stampCount = messages.SelectMany(m => m.Stamps ?? [])
                .Where(s => s.StampId.HasValue)!
                .GroupBy(s => s.StampId!.Value)
                .ToDictionary(g => g.Key, ms => ms.Sum(s => s.Count ?? 0));
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

            await traq.Channels[_postChannelId].Messages.PostAsync(new() { Content = sb.ToString(), Embed = false }, null, ct);
        }
    }

    sealed class StampRankingServiceOptions
    {
        [ConfigurationKeyName("STAMP_RANKING_CHANNEL_ID")]
        public Guid PostChannelId { get; set; }
    }
}
