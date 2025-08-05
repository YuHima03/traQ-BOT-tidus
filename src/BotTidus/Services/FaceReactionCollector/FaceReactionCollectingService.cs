using BotTidus.Configurations;
using BotTidus.Domain;
using BotTidus.Domain.MessageFaceScores;
using BotTidus.Services.ExternalServiceHealthCheck;
using BotTidus.Services.FaceCollector;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Traq;
using Traq.Messages;

namespace BotTidus.Services.FaceReactionCollector
{
    sealed class FaceReactionCollectingService(
        IOptions<TraqBotOptions> botOptions,
        ILogger<FaceReactionCollectingService> logger,
        IRepositoryFactory repoFactory,
        TraqApiClient traq,
        TraqHealthCheckPublisher traqHealthCheck
        )
        : BackgroundService, IHealthCheck
    {
        public TimeSpan Delay { get; } = TimeSpan.FromSeconds(15);
        public TimeSpan Interval { get; } = TimeSpan.FromMinutes(1);

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (traqHealthCheck.CurrentStatus != TraqStatus.Available)
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
                if (traqHealthCheck.CurrentStatus != TraqStatus.Available)
                {
                    logger.LogWarning("The task is skipped because the traQ service is not available.");
                    continue;
                }

                var now = DateTimeOffset.UtcNow;
                MessagesGetResponse messages;
                try
                {
                    messages = await traq.Messages.GetAsMessagesGetResponseAsync(conf => conf.QueryParameters = new() { After = now - TimeSpan.FromMinutes(15), Limit = 100 }, cancellationToken: stoppingToken) ?? throw new Exception("The API response is null");
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to fetch messages.");
                    continue;
                }

                await using var repo = await repoFactory.CreateRepositoryAsync(stoppingToken);
                logger.LogDebug("Collected {Count} messages.", messages.Hits?.Count);

                try
                {
                    if (messages.Hits is not null)
                    {
                        foreach (var m in messages.Hits)
                        {
                            if (m.UserId == botOptions.Value.UserId)
                            {
                                continue;
                            }

                            if (await repo.GetMessageFaceScoreOrDefaultAsync(m.Id.GetValueOrDefault(), stoppingToken) is not null)
                            {
                                continue;
                            }

                            MessageFaceScore? score = null;
                            try
                            {
                                var stamps = m.Stamps!.ToLookup(s => s.StampId.GetValueOrDefault());
                                score = new(
                                    m.Id!.Value,
                                    m.UserId!.Value,
                                    0,
                                    stamps[MessageFaceCounter.NegativeReactionGuid].Count() >= 5 ? 1U : 0,
                                    0,
                                    stamps[MessageFaceCounter.PositiveReactionGuid].Count() >= 5 ? 1U : 0
                                );

                                if (score.NegativeReactionCount == 0 && score.PositiveReactionCount == 0)
                                {
                                    continue;
                                }
                                if (score.NegativeReactionCount != 0)
                                {
                                    await traq.Messages[m.Id!.Value].Stamps[MessageFaceCounter.NegativeReactionGuid].PostAsync(new() { Count = (int)score.NegativeReactionCount }, cancellationToken: stoppingToken);
                                }
                                if (score.PositiveReactionCount != 0)
                                {
                                    await traq.Messages[m.Id!.Value].Stamps[MessageFaceCounter.PositiveReactionGuid].PostAsync(new() { Count = (int)score.PositiveReactionCount }, cancellationToken: stoppingToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Failed to process message stamps: {MessageId}.", m.Id);
                                // Continue because it is not a critical error. (e.g. the message is deleted)
                            }

                            if (score is not null)
                            {
                                try
                                {
                                    await repo.AddMessageFaceScoreAsync(score, stoppingToken);
                                }
                                catch (Exception e)
                                {
                                    logger.LogError(e, "Failed to save face score for message {MessageId} by user {UserId}", m.Id, m.UserId);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process messages.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
