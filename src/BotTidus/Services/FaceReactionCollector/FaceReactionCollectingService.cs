using BotTidus.Domain;
using BotTidus.Domain.MessageFaceScores;
using BotTidus.Services.FaceCollector;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Traq;
using Traq.Model;

namespace BotTidus.Services.FaceReactionCollector
{
    sealed class FaceReactionCollectingService(IOptions<AppConfig> appConfig, ILogger<FaceReactionCollectingService> logger, IRepositoryFactory repoFactory, ITraqApiClient traq) : BackgroundService
    {
        readonly AppConfig _appConfig = appConfig.Value;
        readonly ILogger<FaceReactionCollectingService> _logger = logger;
        readonly IRepositoryFactory _repoFactory = repoFactory;
        readonly ITraqApiClient _traq = traq;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(1));

            do
            {
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

                var repo = await _repoFactory.CreateRepositoryAsync(stoppingToken);
                _logger.LogDebug("Collected {Count} messages.", messages.Hits.Count);

                foreach (var m in messages.Hits)
                {
                    try
                    {
                        if (m.UserId == _appConfig.BotUserId)
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
                        await repo.AddMessageFaceScoreAsync(score, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process message stamps: {MessageId}.", m.Id);
                    }
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
