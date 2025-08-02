using BotTidus.Configurations;
using BotTidus.Domain;
using BotTidus.Services.ExternalServiceHealthCheck;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Traq.Models;

namespace BotTidus.Services.FaceCollector
{
    internal sealed class FaceCollectingService(
        IOptions<TraqBotOptions> botOptions,
        IRepositoryFactory repoFactory,
        TraqHealthCheckPublisher traqHealthCheck,
        ILogger<FaceCollectingService> logger,
        IServiceProvider services
        )
        : RecentMessageCollectingService(services, TimeSpan.FromSeconds(30)), IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (traqHealthCheck.CurrentStatus != TraqStatus.Available)
            {
                return Task.FromResult(HealthCheckResult.Degraded("The traQ service is unavailable."));
            }
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        protected override async ValueTask OnCollectAsync(ActivityTimelineMessage[] messages, CancellationToken ct)
        {
            foreach (var m in messages)
            {
                if (m.UserId == botOptions.Value.UserId)
                {
                    continue;
                }

                await using var repo = await repoFactory.CreateRepositoryAsync(ct);
                var currentTotalCount = (await repo.GetUserFaceCountAsync(m.UserId.GetValueOrDefault(), ct)).TotalScore;
                var (positiveCnt, negativeCnt) = MessageFaceCounter.Count(m.Content.AsSpan(), currentTotalCount);
                if (positiveCnt == 0 && negativeCnt == 0)
                {
                    continue;
                }

                var dbTask = ValueTask.CompletedTask;
                try
                {
                    dbTask = repo.AddMessageFaceScoreAsync(
                        new Domain.MessageFaceScores.MessageFaceScore(
                            m.Id!.Value,
                            m.UserId!.Value,
                            negativeCnt,
                            0,
                            positiveCnt,
                            0),
                        ct);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to add message face score for message {MessageId} by user {UserId}", m.Id, m.UserId);
                }

                if (positiveCnt != 0 && negativeCnt != 0)
                {
                    await Task.WhenAll(
                        dbTask.AsTask(),
                        Client.Messages[m.Id!.Value].Stamps[MessageFaceCounter.PositiveReactionGuid].PostAsync(new() { Count = (int)positiveCnt }, null, ct),
                        Client.Messages[m.Id!.Value].Stamps[MessageFaceCounter.NegativeReactionGuid].PostAsync(new() { Count = (int)negativeCnt }, null, ct)
                    );
                }
                else if (positiveCnt != 0)
                {
                    await Task.WhenAll(
                        dbTask.AsTask(),
                        Client.Messages[m.Id!.Value].Stamps[MessageFaceCounter.PositiveReactionGuid].PostAsync(new() { Count = (int)positiveCnt }, null, ct)
                    );
                }
                else if (negativeCnt != 0)
                {
                    await Task.WhenAll(
                        dbTask.AsTask(),
                        Client.Messages[m.Id!.Value].Stamps[MessageFaceCounter.NegativeReactionGuid].PostAsync(new() { Count = (int)negativeCnt }, null, ct)
                    );
                }
            }
            return;
        }
    }
}
