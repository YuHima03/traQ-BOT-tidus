using BotTidus.Domain;
using BotTidus.Helpers;
using BotTidus.Services.ExternalServiceHealthCheck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Traq.Model;

namespace BotTidus.Services.FaceCollector
{
    internal sealed class FaceCollectingService(IOptions<AppConfig> appConf, IRepositoryFactory repoFactory, IServiceProvider services) : RecentMessageCollectingService(services, TimeSpan.FromSeconds(30)), IHealthCheck
    {
        readonly AppConfig _appConf = appConf.Value;
        readonly IRepositoryFactory _repoFactory = repoFactory;
        readonly TraqHealthCheckPublisher _traqHealthCheck = services.GetRequiredService<TraqHealthCheckPublisher>();

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_traqHealthCheck.CurrentStatus != TraqStatus.Available)
            {
                return Task.FromResult(HealthCheckResult.Degraded("The traQ service is unavailable."));
            }
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        protected override async ValueTask OnCollectAsync(ActivityTimelineMessage[] messages, CancellationToken ct)
        {
            foreach (var m in messages)
            {
                if (m.UserId == _appConf.BotUserId)
                {
                    continue;
                }

                var repo = await _repoFactory.CreateRepositoryAsync(ct);
                var currentTotalCount = (await repo.GetUserFaceCountAsync(m.UserId, ct)).TotalScore;
                var (negativeCnt, positiveCnt) = MessageFaceCounter.Count(m.Content.AsSpan(), currentTotalCount);
                if (positiveCnt == 0 && negativeCnt == 0)
                {
                    continue;
                }

                var dbTask = repo.AddMessageFaceScoreAsync(
                    new Domain.MessageFaceScores.MessageFaceScore(
                        m.Id,
                        m.UserId,
                        negativeCnt,
                        0,
                        positiveCnt,
                        0),
                    ct);

                if (positiveCnt != 0 && negativeCnt != 0)
                {
                    await Task.WhenAll(
                        dbTask.AsTask(),
                        Client.StampApi.AddMessageStampAsync(m.Id, MessageFaceCounter.PositiveReactionGuid, new((int)positiveCnt), ct),
                        Client.StampApi.AddMessageStampAsync(m.Id, MessageFaceCounter.NegativeReactionGuid, new((int)negativeCnt), ct)
                        );
                }
                else if (positiveCnt != 0)
                {
                    await Task.WhenAll(
                        dbTask.AsTask(),
                        Client.StampApi.AddMessageStampAsync(m.Id, MessageFaceCounter.PositiveReactionGuid, new((int)positiveCnt), ct)
                        );
                }
                else if (negativeCnt != 0)
                {
                    await Task.WhenAll(
                        dbTask.AsTask(),
                        Client.StampApi.AddMessageStampAsync(m.Id, MessageFaceCounter.NegativeReactionGuid, new((int)negativeCnt), ct)
                        );
                }
            }
            return;
        }
    }
}
