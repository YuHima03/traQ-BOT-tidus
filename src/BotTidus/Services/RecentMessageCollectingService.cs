using BotTidus.Services.ExternalServiceHealthCheck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotTidus.Services
{
    abstract class RecentMessageCollectingService(IServiceProvider services, TimeSpan interval)
        : PeriodicBackgroundService(Options.Create(new PeriodicBackgroundServiceOptions { Delay = TimeSpan.FromSeconds(15), Period = interval }))
    {
        readonly ILogger<RecentMessageCollectingService> _logger = services.GetRequiredService<ILogger<RecentMessageCollectingService>>();
        readonly TraqHealthCheckPublisher _traqHealthCheck = services.GetRequiredService<TraqHealthCheckPublisher>();

        DateTimeOffset _lastCollectedAt = DateTimeOffset.UtcNow;

        protected Traq.TraqApiClient Client { get; } = services.GetRequiredService<Traq.TraqApiClient>();

        protected sealed override async ValueTask ExecuteCoreAsync(CancellationToken ct)
        {
            if (_traqHealthCheck.CurrentStatus != TraqStatus.Available)
            {
                _logger.LogWarning("The task is skipped because the traQ service is not available.");
                return;
            }

            try
            {
                Traq.Activity.Timeline.TimelineRequestBuilder.TimelineRequestBuilderGetQueryParameters query = new()
                {
                    All = true,
                    Limit = 50
                };
                var timeline = await Client.Activity.Timeline.GetAsync(conf => conf.QueryParameters = query, ct);
                if (timeline is null)
                {
                    _logger.LogError("Failed to fetch activity timeline.");
                    return;
                }
                var messages = timeline.TakeWhile(m => m.CreatedAt > _lastCollectedAt).ToArray();

                MessageLogger.LogCollectedMessages(_logger, messages.Length);

                if (messages.Length != 0)
                {
                    _lastCollectedAt = messages[0].CreatedAt!.Value;
                    await OnCollectAsync(messages, ct);
                }

                timeline.Clear();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to collect messages.");
            }
        }

        protected abstract ValueTask OnCollectAsync(Traq.Models.ActivityTimelineMessage[] messages, CancellationToken ct);
    }
}

static partial class MessageLogger
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Collected {Count} messages.")]
    public static partial void LogCollectedMessages(ILogger logger, int count);
}
