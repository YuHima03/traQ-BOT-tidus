using BotTidus.Domain;
using Microsoft.Extensions.Options;
using Traq.Model;

namespace BotTidus.Services.FaceCollector
{
    internal sealed class FaceCollectingService(IOptions<AppConfig> appConf, IRepositoryFactory repoFactory, IServiceProvider services) : RecentMessageCollectingService(services, TimeSpan.FromSeconds(30))
    {
        readonly AppConfig _appConf = appConf.Value;
        readonly IRepositoryFactory _repoFactory = repoFactory;

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
                var (positiveCnt, negativeCnt) = MessageFaceCounter.Count(m.Content.AsSpan(), currentTotalCount);
                if (positiveCnt == 0 && negativeCnt == 0)
                {
                    continue;
                }

                var dbTask = repo.AddMessageFaceScoreAsync(
                    new Domain.MessageFaceScores.MessageFaceScore(
                        m.Id,
                        m.UserId,
                        positiveCnt,
                        0,
                        negativeCnt,
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
