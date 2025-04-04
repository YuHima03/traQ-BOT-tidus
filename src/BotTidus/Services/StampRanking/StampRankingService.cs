using BotTidus.Helpers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Traq;

namespace BotTidus.Services.StampRanking
{
    internal class StampRankingService(IOptions<AppConfig> appConfig, ILogger<StampRankingService> logger, ITraqApiClient traq) : BackgroundService, IHealthCheck
    {
        readonly ILogger<StampRankingService> _logger = logger;
        readonly Guid _postChannelId = appConfig.Value.StampRankingChannelId;
        readonly ITraqApiClient _traq = traq;

        static DateOnly JstToday => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9).Date);

        static TimeSpan UntilNextJst9AM
        {
            get
            {
                // 09:00:00(JST) = 00:00:00(UTC)
                var utcNow = DateTime.UtcNow;
                return utcNow.AddDays(1).Date - utcNow;
            }
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_postChannelId == Guid.Empty)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Channel to post is not set."));
            }
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_postChannelId == Guid.Empty)
            {
                _logger.LogWarning("Channel to post is not set.");
                return;
            }

            var _delay = UntilNextJst9AM;
            _logger.LogDebug("Waiting for next 9:00 AM JST: {Delay}", _delay);
            await Task.Delay(_delay, stoppingToken);

            using PeriodicTimer timer = new(TimeSpan.FromDays(1));
            do
            {
                var jstYesterday = JstToday.AddDays(-1);

                var jstYesterdayStart = new DateTimeOffset(jstYesterday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), TimeSpan.FromHours(9)); // 00:00:00+09:00
                var jstYesterdayEnd = jstYesterdayStart.AddTicks(TimeSpan.TicksPerDay - 1); // 23:59:59.9999999+09:00

                try
                {
                    var stampNameMap = (await _traq.StampApi.GetStampsAsync(cancellationToken: stoppingToken)).ToDictionary(s => s.Id, s => s.Name);

                    var stampCount = await CollectMessageStampsAsync(jstYesterdayStart, jstYesterdayEnd, stoppingToken);
                    var top100Stamps = stampCount.OrderByDescending(kv => kv.Value).Take(50);

                    StringBuilder sb = new($"""
                    ## {jstYesterdayStart:M/d} Stamp Ranking 50
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

                    await _traq.MessageApi.PostMessageAsync(_postChannelId, new(sb.ToString(), false), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to collect stamps.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        async ValueTask<IEnumerable<KeyValuePair<Guid, int>>> CollectMessageStampsAsync(DateTimeOffset since, DateTimeOffset until, CancellationToken ct)
        {
            if (since > until)
            {
                return [];
            }

            var messages = await _traq.MessageApi.SearchManyMessagesAsync(new() { After = since, Before = until }, ct);
            Dictionary<Guid, int> stampCount = [];

            foreach (var m in messages)
            {
                foreach (var s in m.Stamps)
                {
                    if (stampCount.TryGetValue(s.StampId, out var cnt))
                    {
                        stampCount[s.StampId] = cnt + s.Count;
                    }
                    else
                    {
                        stampCount[s.StampId] = s.Count;
                    }
                }
            }

            return stampCount;
        }
    }
}
