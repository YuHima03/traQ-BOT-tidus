using BotTidus.Configurations;
using BotTidus.Domain;
using BotTidus.Domain.MessageFaceScores;
using BotTidus.Services.ExternalServiceHealthCheck;
using BotTidus.Services.FaceCollector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Traq;
using Traq.Model;

namespace BotTidus.Services.FaceReactionCollector
{
    sealed class FaceReactionCollectingService(IOptions<TraqBotOptions> botOptions, ILogger<FaceReactionCollectingService> logger, IRepositoryFactory repoFactory, ITraqApiClient traq, IServiceProvider services) : BackgroundService, IHealthCheck
    {
        readonly TraqBotOptions _botOptions = botOptions.Value;
        readonly ILogger<FaceReactionCollectingService> _logger = logger;
        readonly IRepositoryFactory _repoFactory = repoFactory;
        readonly ITraqApiClient _traq = traq;
        readonly TraqHealthCheckPublisher _traqHealthCheck = services.GetRequiredService<TraqHealthCheckPublisher>();

        public TimeSpan Delay { get; } = TimeSpan.FromSeconds(15);
        public TimeSpan Interval { get; } = TimeSpan.FromMinutes(1);

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_traqHealthCheck.CurrentStatus != TraqStatus.Available)
            {
                return Task.FromResult(HealthCheckResult.Degraded("The traQ service is unavailable."));
            }
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(Delay, stoppingToken);
            using PeriodicTimer timer = new(Interval);

            do
            {
                if (_traqHealthCheck.CurrentStatus != TraqStatus.Available)
                {
                    _logger.LogWarning("The task is skipped because the traQ service is not available.");
                    continue;
                }

                var now = DateTimeOffset.UtcNow;
                MessageSearchResult messages;
                try
                {
                    messages = await _traq.MessageApi.SearchMessagesAsync(after: now - TimeSpan.FromMinutes(15), limit: 100, cancellationToken: stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to fetch messages.");
                    continue;
                }

                await using var repo = await _repoFactory.CreateRepositoryAsync(stoppingToken);
                _logger.LogDebug("Collected {Count} messages.", messages.Hits.Count);

                try
                {
                    foreach (var m in messages.Hits)
                    {
                        if (m.UserId == _botOptions.UserId)
                        {
                            continue;
                        }

                        if (await repo.GetMessageFaceScoreOrDefaultAsync(m.Id, stoppingToken) is not null)
                        {
                            continue;
                        }

                        var stamps = m.Stamps.ToLookup(s => s.StampId);
                        MessageFaceScore score = new(
                            m.Id,
                            m.UserId,
                            0,
                            stamps[MessageFaceCounter.NegativeReactionGuid].Count() >= 5 ? 1U : 0,
                            0,
                            stamps[MessageFaceCounter.PositiveReactionGuid].Count() >= 5 ? 1U : 0
                            );

                        try
                        {
                            if (score.NegativeReactionCount == 0 && score.PositiveReactionCount == 0)
                            {
                                continue;
                            }
                            if (score.NegativeReactionCount != 0)
                            {
                                await _traq.StampApi.AddMessageStampAsync(m.Id, MessageFaceCounter.NegativeReactionGuid, new PostMessageStampRequest(1), stoppingToken);
                            }
                            if (score.PositiveReactionCount != 0)
                            {
                                await _traq.StampApi.AddMessageStampAsync(m.Id, MessageFaceCounter.PositiveReactionGuid, new PostMessageStampRequest(1), stoppingToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to process message stamps: {MessageId}.", m.Id);
                            // Continue because it is not a critical error. (e.g. the message is deleted)
                        }
                        await repo.AddMessageFaceScoreAsync(score, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process messages.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
