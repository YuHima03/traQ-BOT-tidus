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

        static DateTimeOffset JstToday => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9)).Date; // 00:00:00.0000000+09:00

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
                var jstYesterdayStart = JstToday.AddDays(-1);                               // 00:00:00.0000000+09:00
                var jstYesterdayEnd = jstYesterdayStart.AddTicks(TimeSpan.TicksPerDay - 1); // 23:59:59.9999999+09:00

                if (_traqHealthCheck.CurrentStatus != TraqStatus.Available)
                {
                    _logger.LogWarning("The task (stamp ranking {Date:M/d}) is skipped because the traQ service is not available.", jstYesterdayStart);
                    continue;
                }

                try
                {
                    var cacheKey = $"DailyMessages:{jstYesterdayStart:yyyyMMdd}";
                    if (!_cache.TryGetValue(cacheKey, out Traq.Model.Message[]? messages) || messages is null)
                    {
                        messages = [.. await _traq.MessageApi.SearchManyMessagesAsync(new() { After = jstYesterdayStart, Before = jstYesterdayEnd }, stoppingToken)];
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
