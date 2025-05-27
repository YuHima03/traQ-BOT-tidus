using BotTidus.Services.ExternalServiceHealthCheck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotTidus.Services
{
    internal abstract class RecentMessageCollectingService(IServiceProvider services, TimeSpan interval)
        : PeriodicBackgroundService(Options.Create(new PeriodicBackgroundServiceOptions { Delay = TimeSpan.FromSeconds(15), Period = interval }))
    {
        readonly ILogger<RecentMessageCollectingService> _logger = services.GetRequiredService<ILogger<RecentMessageCollectingService>>();
        readonly TraqHealthCheckPublisher _traqHealthCheck = services.GetRequiredService<TraqHealthCheckPublisher>();

        DateTimeOffset _lastCollectedAt = DateTimeOffset.UtcNow;

        protected Traq.ITraqApiClient Client { get; } = services.GetRequiredService<Traq.ITraqApiClient>();

        protected sealed override async ValueTask ExecuteCoreAsync(CancellationToken ct)
        {
            if (_traqHealthCheck.CurrentStatus != TraqStatus.Available)
            {
                _logger.LogWarning("The task is skipped because the traQ service is not available.");
                return;
            }

            try
            {
                var timeline = await Client.ActivityApi.GetActivityTimelineAsync(limit: 50, all: true, cancellationToken: ct);
                var messages = timeline.TakeWhile(m => m.CreatedAt > _lastCollectedAt).ToArray();

                _logger.LogDebug("Collected {Count} messages.", messages.Length);

                if (messages.Length != 0)
                {
                    _lastCollectedAt = messages[0].CreatedAt;
                    await OnCollectAsync(messages, ct);
                }

                timeline.Clear();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to collect messages.");
            }
        }

        protected abstract ValueTask OnCollectAsync(Traq.Model.ActivityTimelineMessage[] messages, CancellationToken ct);
    }
}
