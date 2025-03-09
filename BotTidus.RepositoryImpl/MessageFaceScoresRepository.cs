using BotTidus.Domain.MessageFaceScores;
using Microsoft.EntityFrameworkCore;

namespace BotTidus.RepositoryImpl
{
    public sealed partial class Repository
    {
        ValueTask IMessageFaceScoresRepository.AddMessageFaceScoreAsync(MessageFaceScore score, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        ValueTask<MessageFaceScore> IMessageFaceScoresRepository.GetMessageFaceScoreAsync(Guid id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        async ValueTask<MessageFaceScore[]> IMessageFaceScoresRepository.GetMessageFaceScoresByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await MessageFaceScores
                .Where(r => r.UserId == userId)
                .Select(r => new MessageFaceScore(r.MessageId, r.UserId, r.NegativePhraseCount, r.NegativeReactionCount, r.PositivePhraseCount, r.PositiveReactionCount))
                .ToArrayAsync(ct);
        }

        async ValueTask<KeyValuePair<Guid, int>[]> IMessageFaceScoresRepository.GetUserFaceCountsAsync(CancellationToken ct)
        {
            return await MessageFaceScores
                .GroupBy(s => s.UserId)
                .Select(g => KeyValuePair.Create(
                    g.Key,
                    g.Select(s => (int)(s.PositivePhraseCount + s.PositiveReactionCount) - (int)(s.NegativePhraseCount + s.NegativeReactionCount)).Sum() + 1
                    ))
                .ToArrayAsync(ct);
        }

        ValueTask IMessageFaceScoresRepository.UpdateMessageFaceScoreAsync(MessageFaceScore score, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
