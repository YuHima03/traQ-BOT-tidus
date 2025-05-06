using BotTidus.Helpers;
using BotTidus.Services.ExternalServiceHealthCheck;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Traq;

namespace BotTidus.Services
{
    internal abstract class DailyMessageCollectingService : BackgroundService
    {
        readonly IMemoryCache _cache;
        readonly ILogger<DailyMessageCollectingService> _logger;
        readonly TimeSpan _startDelay;
        readonly ITraqApiClient _traq;
        readonly TraqHealthCheckPublisher _traqHealthCheck;

        static readonly TimeZoneInfo TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

        public DailyMessageCollectingService(IServiceProvider services, TimeSpan startDelay)
        {
            if (startDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(startDelay), "The time span must be non-negative.");
            }
            _cache = services.GetRequiredService<IMemoryCache>();
            _logger = services.GetRequiredService<ILogger<DailyMessageCollectingService>>();
            _startDelay = startDelay;
            _traq = services.GetRequiredService<ITraqApiClient>();
            _traqHealthCheck = services.GetRequiredService<TraqHealthCheckPublisher>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(_startDelay, stoppingToken);

            PeriodicTimer timer = new(TimeSpan.FromDays(1));
            do
            {
                var yesterdayStart = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo).Date.AddDays(-1);
                var yesterdayEnd = yesterdayStart.AddDays(1).AddTicks(-1);

                if (_traqHealthCheck.CurrentStatus != TraqStatus.Available)
                {
                    _logger.LogWarning("The task (stamp ranking {Date:M/d}) is skipped because the traQ service is not available.", yesterdayStart);
                    continue;
                }

                try
                {
                    var cacheKey = $"services.dailyMessageCollector:{yesterdayStart:yyyyMMdd}";
                    if (!_cache.TryGetValue(cacheKey, out Traq.Model.Message[]? messages) || messages is null)
                    {
                        messages = [.. await _traq.MessageApi.SearchManyMessagesAsync(new() { After = TimeZoneInfo.ConvertTimeToUtc(yesterdayStart, TimeZoneInfo), Before = TimeZoneInfo.ConvertTimeToUtc(yesterdayEnd, TimeZoneInfo) }, stoppingToken)];
                        _cache.Set(cacheKey, messages, TimeSpan.FromMinutes(5));
                    }
                    await OnCollectAsync(messages, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to collect messages.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        protected abstract ValueTask OnCollectAsync(Traq.Model.Message[] messages, CancellationToken ct);
    }
}
