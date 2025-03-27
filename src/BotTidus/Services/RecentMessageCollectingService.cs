﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotTidus.Services
{
    internal abstract class RecentMessageCollectingService(IServiceProvider services, TimeSpan interval) : BackgroundService
    {
        readonly ILogger<RecentMessageCollectingService> _logger = services.GetRequiredService<ILogger<RecentMessageCollectingService>>();

        protected Traq.ITraqApiClient Client { get; } = services.GetRequiredService<Traq.ITraqApiClient>();

        public TimeSpan Interval { get; } = interval.Duration();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(Interval);
            DateTimeOffset lastCollectedAt = DateTimeOffset.UtcNow;

            _logger.LogDebug("Started collecting messages.");

            do
            {
                var timeline = await Client.ActivityApi.GetActivityTimelineAsync(limit: 50, all: true, cancellationToken: stoppingToken);
                var messages = timeline.TakeWhile(m => m.CreatedAt > lastCollectedAt).ToArray();

                _logger.LogDebug("Collected {Count} messages.", messages.Length);

                if (messages.Length != 0)
                {
                    lastCollectedAt = messages[0].CreatedAt;
                    await OnCollectAsync(messages, stoppingToken);
                }

                timeline.Clear();
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        protected abstract ValueTask OnCollectAsync(Traq.Model.ActivityTimelineMessage[] messages, CancellationToken ct);
    }
}
