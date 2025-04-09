using BotTidus.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Traq;
using Traq.Model;

namespace BotTidus.Services.StampRanking
{
    internal class StampRankingService(IServiceProvider services)
        : DailyMessageCollectingService(services, TimeHelper.GetTimeSpanUntilNextTime(TimeOnly.MinValue)), // Wait until next 09:00:00(JST)
          IHealthCheck
    {
        readonly ILogger<StampRankingService> _logger = services.GetRequiredService<ILogger<StampRankingService>>();
        readonly Guid _postChannelId = services.GetRequiredService<IOptions<AppConfig>>().Value.StampRankingChannelId;
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
            var stampNameMap = (await _traq.StampApi.GetStampsAsync(cancellationToken: ct)).ToDictionary(s => s.Id, s => s.Name);
            var stampCount = messages.SelectMany(m => m.Stamps).GroupBy(s => s.StampId).ToDictionary(g => g.Key, ms => ms.Sum(s => s.Count));
            var top100Stamps = stampCount.OrderByDescending(kv => kv.Value).Take(50);

            var yesterday = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9)).Date.AddDays(-1);
            StringBuilder sb = new($"""
                    ## {yesterday:M/d} Stamp Ranking 50 だよ！
                    | rank | stamp | count |
                    |-----:|:------|------:|
                    """);
            sb.AppendLine();

            int prevCount = int.MaxValue;
            int rank = 1;
            foreach (var (stampId, count) in top100Stamps)
            {
                if (!stampNameMap.TryGetValue(stampId, out var stampName))
                {
                    _logger.LogWarning("Unknown stamp: {}", stampId);
                    continue;
                }

                if (prevCount == count)
                {
                    sb.AppendLine($"| - | :{stampName}: `{stampName}` | {count} |");
                }
                else
                {
                    sb.AppendLine($"| {rank} | :{stampName}: `{stampName}` | {count} |");
                    rank++;
                }
                prevCount = count;
            }

            await _traq.MessageApi.PostMessageAsync(_postChannelId, new PostMessageRequest(sb.ToString(), false), ct);
        }
    }
}
